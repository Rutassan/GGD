using System;
using System.Diagnostics.CodeAnalysis;
using Raylib_cs;

public class Player
{
    public int X { get; set; }
    public int Y { get; set; }
    public char Character { get; set; }
    public Color Color { get; set; }
    public Inventory PlayerInventory { get; private set; }
    public bool IsFreezing { get; set; } // Новое свойство для состояния "замерзания"
    public const int HungerDecayInterval = 1080;
    private const int HungerRecoveryPerFood = 30;
    public const int MaxHunger = 100;
    private int _hungerDecayCounter;
    public int Hunger { get; private set; }
    public bool IsStarving => Hunger <= 0;
    public bool IsHungry => Hunger <= 30;
    public const int MaxHealth = 100;
    public int Health { get; private set; }
    public bool IsAlive => Health > 0;
    public const int FreezeDamageInterval = 180;
    public const int HealthRegenInterval = 240;
    private int _freezeDamageCounter;
    private int _healthRegenCounter;

    public Player(int x, int y, char character, Color color)
    {
        X = x;
        Y = y;
        Character = character;
        Color = color;
        PlayerInventory = new Inventory();
        IsFreezing = false; // Инициализируем по умолчанию
        Hunger = MaxHunger;
        _hungerDecayCounter = 0;
        Health = MaxHealth;
        _freezeDamageCounter = 0;
        _healthRegenCounter = 0;
    }

    [ExcludeFromCodeCoverage]
    public void Draw(int fontSize, int cameraX, int cameraY)
    {
        // Отрисовываем игрока, смещенного относительно позиции камеры
        Raylib.DrawText(Character.ToString(), (X - cameraX) * fontSize, (Y - cameraY) * fontSize, fontSize, Color);
    }

    public virtual bool Move(int dx, int dy, GameMap map)
    {
        int newX = X + dx;
        int newY = Y + dy;
        bool isWall = map.IsWall(newX, newY);
        if (!isWall)
        {
            X = newX;
            Y = newY;
            return true;
        }
        return false;
    }

    public void Mine(GameMap map)
    {
        MapCell currentCell = map.GetCell(X, Y);
        if (currentCell.Resource != null)
        {
            currentCell.Resource.Health--;
            if (currentCell.Resource.Health <= 0)
            {
                PlayerInventory.AddItem(currentCell.Resource.ResourceType, 1);
                map.SetCell(X, Y, MapCell.Empty()); // Заменяем добытый ресурс на пустую ячейку
            }
        }
    }

    public bool Eat(string resourceType)
    {
        if (!PlayerInventory.HasItem(resourceType, 1))
        {
            return false;
        }
        PlayerInventory.RemoveItem(resourceType, 1);
        Hunger = Math.Min(MaxHunger, Hunger + HungerRecoveryPerFood);
        return true;
    }

    public void UpdateHunger()
    {
        _hungerDecayCounter++;
        if (_hungerDecayCounter >= HungerDecayInterval)
        {
            Hunger = Math.Max(0, Hunger - 1);
            _hungerDecayCounter = 0;
        }
    }

    public void UpdateSurvivalStats(bool isInside)
    {
        UpdateHunger();

        if (IsFreezing && !isInside)
        {
            _healthRegenCounter = 0;
            _freezeDamageCounter++;
            if (_freezeDamageCounter >= FreezeDamageInterval)
            {
                TakeDamage(1);
                _freezeDamageCounter = 0;
            }
            return;
        }

        _freezeDamageCounter = 0;
        if (Hunger > MaxHunger / 2)
        {
            _healthRegenCounter++;
            if (_healthRegenCounter >= HealthRegenInterval && Health < MaxHealth)
            {
                RestoreHealth(1);
                _healthRegenCounter = 0;
            }
        }
        else
        {
            _healthRegenCounter = 0;
        }
    }

    public bool Build(GameMap map, string resourceType, int targetX, int targetY)
    {
        // Пока что строим только стены из "Stone"
        if (resourceType == "Stone" && PlayerInventory.HasItem("Stone", 1))
        {
            // Проверяем, что целевая ячейка пуста или не является стеной, чтобы можно было строить
            MapCell targetCell = map.GetCell(targetX, targetY);
            if (!targetCell.IsWall && targetCell.Resource == null) // Проверяем, что ресурс null, чтобы не заменять его
            {
                PlayerInventory.RemoveItem("Stone", 1); // THIS SHOULD REMOVE THE STONE
                map.SetCell(targetX, targetY, MapCell.Wall()); // Строим стену
                return true;
            }
        }
        return false;
    }

    public bool BuildDoor(GameMap map, int targetX, int targetY)
    {
        if (!PlayerInventory.HasItem("Stone", 1))
        {
            return false;
        }

        if (map.PlaceDoor(targetX, targetY))
        {
            PlayerInventory.RemoveItem("Stone", 1);
            return true;
        }
        return false;
    }

    public void TakeDamage(int amount)
    {
        if (amount <= 0)
        {
            return;
        }
        Health = Math.Max(0, Health - amount);
    }

    public void RestoreHealth(int amount)
    {
        if (amount <= 0)
        {
            return;
        }
        Health = Math.Min(MaxHealth, Health + amount);
    }
}
