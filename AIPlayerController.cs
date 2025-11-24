public class AIPlayerController : IPlayerController
{
    private enum AIState
    {
        Idle,
        ExecutingPath,
        SearchingForResource,
        Mining,
        Building,
        SeekingShelter,
        WaitingInShelter // Новое состояние: ИИ прячется в убежище
    }

    private enum ShelterBuildState
    {
        None,
        FindingSpot,
        CollectingResources,
        BuildingWalls,
        EnteringShelter
    }

    private readonly Random _random = new Random();
    private int _actionDelayCounter = 0;
    private const int _actionDelayBase = 20; // Базовая задержка между действиями
    private const int _shelterSeekThreshold = 600; // За сколько кадров до ночи ИИ начинает искать убежище
    private (int x, int y) _shelterTarget = (-1, -1); // Куда ИИ пытается попасть для укрытия

    private List<(int, int)> _path = new List<(int, int)>();
    private (int x, int y)? _targetResourcePosition = null;
    private AIState _currentState = AIState.Idle;
    private readonly Func<GameMap, int, int, int, int, List<(int, int)>?> _pathfinder;

    private ShelterBuildState _shelterBuildState = ShelterBuildState.None;
    private (int x, int y)? _shelterSpotCenter = null;
    private Queue<(int x, int y)> _buildQueue = new Queue<(int x, int y)>();
    private const int _shelterSize = 3; // Например, 3x3 внутреннее пространство

    public AIPlayerController(Func<GameMap, int, int, int, int, List<(int, int)>?>? pathfinder = null)
    {
        _pathfinder = pathfinder ?? Pathfinder.FindPath;
    }

    public void Update(Player player, GameMap map, bool isDay, long gameTime)
    {
        int currentActionDelay = _actionDelayBase;
        if (player.IsFreezing)
        {
            currentActionDelay *= 3; // Увеличиваем задержку, если игрок замерзает
        }

        _actionDelayCounter++;

        if (_actionDelayCounter < currentActionDelay)
        {
            return;
        }

        _actionDelayCounter = 0;

        // Если ИИ находится внутри и сейчас ночь, он просто ждет
        if (!isDay && map.IsInside(player.X, player.Y))
        {
            _currentState = AIState.WaitingInShelter;
            _path.Clear(); // Отменяем все текущие планы
            _shelterBuildState = ShelterBuildState.None; // Сброс состояния строительства
            return;
        }
        
        long currentCycleTime = gameTime % Program.TotalDayNightDuration;
        bool isNight = currentCycleTime >= Program.DayDuration;
        bool approachingNight = currentCycleTime >= Program.DayDuration - _shelterSeekThreshold && currentCycleTime < Program.DayDuration;
        bool nightApproaching = isNight || approachingNight;

        if (nightApproaching && !map.IsInside(player.X, player.Y))
        {
            _currentState = AIState.SeekingShelter;
            if (HandleShelterLogic(player, map))
            {
                // Если логика убежища активна и генерирует путь, следуем ему
                if (_path.Count > 0)
                {
                    ExecuteNextStep(player, map);
                    return;
                }
                // Если убежище построено, но ИИ не внутри, пытаемся войти
                if (map.IsInside(player.X, player.Y))
                {
                    _shelterBuildState = ShelterBuildState.None;
                    return;
                }
            }
            else // Если не удалось найти или построить убежище, продолжаем блуждать/добывать, но с высокой задержкой
            {
                 // На этот случай можно реализовать более паническое поведение или просто случайное движение
                 // Пока что просто проваливаемся в обычную логику, но в условиях замерзания действия замедлены
            }
        } else {
            _shelterBuildState = ShelterBuildState.None; // Сбрасываем состояние строительства, если день
        }


        // Обновляем состояние цели (ресурса)
        if (_targetResourcePosition.HasValue)
        {
            var target = _targetResourcePosition.Value;
            MapCell targetCell = map.GetCell(target.x, target.y);
            if (targetCell.Resource == null || targetCell.Resource.Health <= 0)
            {
                _targetResourcePosition = null;
                _path.Clear();
            }
        }
        
        // Обычная логика, если не нужно строить убежище или оно уже построено
        
        // 1. Приоритет — добыча, если стоим на ресурсе
        if (TryMineCurrentCell(player, map))
        {
            return;
        }

        // 2. Если есть актуальный маршрут — двигаемся
        if (_path.Count > 0)
        {
            _currentState = AIState.ExecutingPath;
            ExecuteNextStep(player, map);
            return;
        }

        // 2.1. Если маршрута нет, пытаемся найти ближайший ресурс и сразу двинуться к нему
        if (TryPlanPathToNearestResource(player, map))
        {
            _currentState = AIState.ExecutingPath;
            ExecuteNextStep(player, map);
            return;
        }

        // 3. Пытаемся построить, если есть камень и клетка пустая
        MapCell currentCell = map.GetCell(player.X, player.Y);
        if (player.PlayerInventory.HasItem("Stone", 1)
            && !currentCell.IsWall
            && currentCell.Resource == null)
        {
            _currentState = AIState.Building;
            player.Build(map, "Stone", player.X, player.Y);
            return;
        }

        // 4. Режим ожидания — случайное блуждание, чтобы не застревать
        WanderRandomly(player, map);
    }

    private bool HandleShelterLogic(Player player, GameMap map)
    {
        switch (_shelterBuildState)
        {
            case ShelterBuildState.None:
                _shelterBuildState = ShelterBuildState.FindingSpot;
                _path.Clear();
                goto case ShelterBuildState.FindingSpot;

            case ShelterBuildState.FindingSpot:
                _shelterSpotCenter = FindOpenShelterSpot(map, player.X, player.Y, _shelterSize);
                if (_shelterSpotCenter.HasValue)
                {
                    _shelterBuildState = ShelterBuildState.CollectingResources;
                    goto case ShelterBuildState.CollectingResources;
                }
                else
                {
                    // Если не нашли место, отменяем попытку или продолжаем искать (в данном случае просто сброс)
                    _shelterBuildState = ShelterBuildState.None;
                    return false;
                }

            case ShelterBuildState.CollectingResources:
                if (player.PlayerInventory.HasItem("Stone", _shelterSize * _shelterSize - 1)) // Например, для стен
                {
                    _shelterBuildState = ShelterBuildState.BuildingWalls;
                    _buildQueue = PlanBuildingSequence(map, _shelterSpotCenter.Value, _shelterSize);
                    goto case ShelterBuildState.BuildingWalls;
                }
                else
                {
                    // Нужно добыть камень
                    if (!player.PlayerInventory.HasItem("Stone", 1)) // Если нет камня вообще, ищем его
                    {
                        if (TryPlanPathToNearestResource(player, map))
                        {
                            return true; // Идем добывать
                        }
                    }
                    // Если камень есть, но недостаточно для строительства, продолжаем добывать
                    if (TryMineCurrentCell(player, map))
                    {
                        return true;
                    }
                    else // Если не можем добыть и не можем найти, что-то пошло не так
                    {
                        _shelterBuildState = ShelterBuildState.None;
                        return false;
                    }
                }

            case ShelterBuildState.BuildingWalls:
                if (_buildQueue.Count > 0)
                {
                    var (targetX, targetY) = _buildQueue.Peek(); // Смотрим следующую точку для строительства

                    // Проверяем, нужно ли двигаться к месту строительства
                    if (player.X != targetX || player.Y != targetY)
                    {
                        _path = _pathfinder(map, player.X, player.Y, targetX, targetY) ?? new List<(int, int)>();
                        if (_path.Count > 0)
                        {
                            if (_path[0].Item1 == player.X && _path[0].Item2 == player.Y) _path.RemoveAt(0);
                            return true; // Двигаемся к точке
                        }
                        else
                        {
                            // Если не можем добраться до точки строительства, возможно она заблокирована
                            _buildQueue.Dequeue(); // Пропускаем эту точку
                            return true; // Продолжаем строить
                        }
                    }
                    else // Игрок находится в нужной точке, строим
                    {
                        if (player.Build(map, "Stone", targetX, targetY))
                        {
                            _buildQueue.Dequeue(); // Успешно построили, удаляем из очереди
                            return true;
                        }
                        else
                        {
                            // Не смогли построить, возможно, нет ресурсов или клетка занята
                            _buildQueue.Dequeue(); // Пропускаем
                            return true;
                        }
                    }
                }
                else
                {
                    // Все стены построены, теперь нужно убедиться, что игрок внутри
                    _shelterBuildState = ShelterBuildState.EnteringShelter;
                    goto case ShelterBuildState.EnteringShelter;
                }

            case ShelterBuildState.EnteringShelter:
                if (map.IsInside(player.X, player.Y))
                {
                    _shelterBuildState = ShelterBuildState.None; // Цель достигнута
                    return true;
                }
                else
                {
                    // Ищем путь к центру убежища или к ближайшей внутренней точке
                    _path = _pathfinder(map, player.X, player.Y, _shelterSpotCenter.Value.x, _shelterSpotCenter.Value.y) ?? new List<(int, int)>();
                    if (_path.Count > 0)
                    {
                        if (_path[0].Item1 == player.X && _path[0].Item2 == player.Y) _path.RemoveAt(0);
                        return true;
                    }
                    else
                    {
                        // Не можем войти, что-то пошло не так
                        _shelterBuildState = ShelterBuildState.None;
                        return false;
                    }
                }
        }
        return false;
    }

    private (int x, int y)? FindOpenShelterSpot(GameMap map, int playerX, int playerY, int size)
    {
        // Поиск открытого квадрата (size+2)x(size+2) для строительства убежища с внутренним пространством size x size
        // (size+2) потому что это размер внешних стен + внутреннее пространство.
        int requiredWidth = size + 2;
        int requiredHeight = size + 2;

        for (int y = 0; y <= GameMap.MapHeight - requiredHeight; y++)
        {
            for (int x = 0; x <= GameMap.MapWidth - requiredWidth; x++)
            {
                bool isSpotOpen = true;
                for (int ty = 0; ty < requiredHeight; ty++)
                {
                    for (int tx = 0; tx < requiredWidth; tx++)
                    {
                        // Проверяем, чтобы вся область была свободна от стен и ресурсов
                        if (map.IsWall(x + tx, y + ty) || map.GetCell(x + tx, y + ty).Resource != null)
                        {
                            isSpotOpen = false;
                            break;
                        }
                    }
                    if (!isSpotOpen) break;
                }

                if (isSpotOpen)
                {
                    // Возвращаем центр внутреннего пространства
                    return (x + 1 + size / 2, y + 1 + size / 2);
                }
            }
        }
        return null;
    }

    private Queue<(int x, int y)> PlanBuildingSequence(GameMap map, (int x, int y) center, int size)
    {
        Queue<(int x, int y)> buildQueue = new Queue<(int x, int y)>();
        int startX = center.x - size / 2 - 1;
        int startY = center.y - size / 2 - 1;

        // Строим внешние стены 
        for (int dy = 0; dy < size + 2; dy++)
        {
            for (int dx = 0; dx < size + 2; dx++)
            {
                if (dx == 0 || dx == size + 1 || dy == 0 || dy == size + 1) // Это стена
                {
                    buildQueue.Enqueue((startX + dx, startY + dy));
                }
            }
        }
        return buildQueue;
    }

    private (int x, int y)? FindNearestSafeSpot(GameMap map, int startX, int startY)
    {
        Queue<(int x, int y, int dist)> queue = new Queue<(int x, int y, int dist)>();
        HashSet<(int x, int y)> visited = new HashSet<(int x, int y)>();

        queue.Enqueue((startX, startY, 0));
        visited.Add((startX, startY));

        int[] dx = { 0, 0, 1, -1 };
        int[] dy = { 1, -1, 0, 0 };

        while (queue.Count > 0)
        {
            var (currX, currY, currDist) = queue.Dequeue();

            // Проверяем, является ли текущая позиция "безопасной"
            // Безопасная позиция - это точка внутри полностью замкнутого контура
            if (map.IsInside(currX, currY))
            {
                return (currX, currY);
            }

            for (int i = 0; i < 4; i++)
            {
                int nextX = currX + dx[i];
                int nextY = currY + dy[i];

                if (nextX >= 0 && nextX < GameMap.MapWidth && nextY >= 0 && nextY < GameMap.MapHeight &&
                    !map.IsWall(nextX, nextY) && !visited.Contains((nextX, nextY)))
                {
                    visited.Add((nextX, nextY));
                    queue.Enqueue((nextX, nextY, currDist + 1));
                }
            }
        }
        return null;
    }

    // Вспомогательный метод для поиска ближайшего ресурса
    private (int x, int y)? FindNearestResource(GameMap map, int startX, int startY, string resourceType)
    {
        Queue<(int x, int y, int dist)> queue = new Queue<(int x, int y, int dist)>();
        HashSet<(int x, int y)> visited = new HashSet<(int x, int y)>();

        queue.Enqueue((startX, startY, 0));
        visited.Add((startX, startY));

        (int x, int y)? nearestResource = null;
        int minDistance = int.MaxValue;

        while (queue.Count > 0)
        {
            (int currX, int currY, int currDist) = queue.Dequeue();

            MapCell cell = map.GetCell(currX, currY);
            if (cell.Resource != null && cell.Resource.ResourceType == resourceType && cell.Resource.Health > 0)
            {
                if (nearestResource == null || currDist < minDistance)
                {
                    minDistance = currDist;
                    nearestResource = (currX, currY);
                }
            }

            int[] dx = { 0, 0, 1, -1 };
            int[] dy = { 1, -1, 0, 0 };

            for (int i = 0; i < 4; i++)
            {
                int nextX = currX + dx[i];
                int nextY = currY + dy[i];

                if (nextX >= 0 && nextX < GameMap.MapWidth && nextY >= 0 && nextY < GameMap.MapHeight &&
                    !map.IsWall(nextX, nextY) && !visited.Contains((nextX, nextY)))
                {
                    visited.Add((nextX, nextY));
                    queue.Enqueue((nextX, nextY, currDist + 1));
                }
            }
        }
        return nearestResource;
    }

    private bool TryMineCurrentCell(Player player, GameMap map)
    {
        MapCell currentCell = map.GetCell(player.X, player.Y);
        if (currentCell.Resource != null && currentCell.Resource.ResourceType == "Stone" && currentCell.Resource.Health > 0)
        {
            _currentState = AIState.Mining;
            player.Mine(map);
            _targetResourcePosition = null;
            _path.Clear();
            return true;
        }
        return false;
    }

    private bool TryPlanPathToNearestResource(Player player, GameMap map)
    {
        _targetResourcePosition = FindNearestResource(map, player.X, player.Y, "Stone");
        if (!_targetResourcePosition.HasValue)
        {
            _path.Clear();
            return false;
        }

        List<(int, int)>? newPath = _pathfinder(map, player.X, player.Y, _targetResourcePosition.Value.x, _targetResourcePosition.Value.y);
        if (newPath == null || newPath.Count == 0)
        {
            _targetResourcePosition = null;
            _path.Clear();
            return false;
        }

        _path = newPath;
        if (_path.Count > 0 && _path[0].Item1 == player.X && _path[0].Item2 == player.Y)
        {
            _path.RemoveAt(0); // Игнорируем текущую клетку в маршруте
        }
        // Если путь ведет к цели, которая стала стеной, очистить путь
        if (_path.Count > 0 && map.IsWall(_path[_path.Count - 1].Item1, _path[_path.Count - 1].Item2))
        {
            _path.Clear();
            _targetResourcePosition = null;
            return false;
        }

        return _path.Count > 0;
    }

    private void ExecuteNextStep(Player player, GameMap map)
    {
        if (_path.Count == 0)
        {
            return;
        }

        int nextX = _path[0].Item1;
        int nextY = _path[0].Item2;
        int dx = nextX - player.X;
        int dy = nextY - player.Y;

        if (player.Move(dx, dy, map))
        {
            _path.RemoveAt(0);
        }
        else
        {
            // Если не удалось пройти (например, стена появилась) - очищаем путь
            _path.Clear();
            _targetResourcePosition = null;
            _currentState = AIState.Idle;
        }
    }

    private void WanderRandomly(Player player, GameMap map)
    {
        _currentState = AIState.Idle;
        int rand_dx = 0, rand_dy = 0;
        int direction = _random.Next(4);
        switch (direction)
        {
            case 0: rand_dy = -1; break;
            case 1: rand_dy = 1; break;
            case 2: rand_dx = -1; break;
            case 3: rand_dx = 1; break;
        }
        player.Move(rand_dx, rand_dy, map);
    }
}
