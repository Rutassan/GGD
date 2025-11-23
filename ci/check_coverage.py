import sys
import xml.etree.ElementTree as ET

def check_coverage(xml_file, threshold):
    try:
        tree = ET.parse(xml_file)
        root = tree.getroot()
        
        # Cobertura XML format has a 'line-rate' attribute in the 'coverage' element
        # Or 'lines-covered' and 'lines-valid' in 'summary' element for lcov format.
        # Assuming Cobertura format based on CoverletOutputFormat=cobertura
        
        # The 'line-rate' attribute is directly on the 'coverage' node for the overall report
        # Or sometimes on the 'sources' node, or 'package' node.
        # Let's find the first 'coverage' node which might contain the line-rate
        
        # Coverlet's Cobertura format usually has it on the top-level 'coverage' element
        line_rate_str = root.get('line-rate')
        if line_rate_str is None:
            # Fallback if not on root, try to find a package level line-rate
            package = root.find('packages/package')
            if package is not None:
                line_rate_str = package.get('line-rate')

        if line_rate_str is None:
            print(f"Error: Could not find 'line-rate' attribute in {xml_file}", file=sys.stderr)
            sys.exit(1)

        line_rate = float(line_rate_str)
        percentage = line_rate * 100

        print(f"Current Line Coverage: {percentage:.2f}%")

        if percentage < threshold:
            print(f"Error: Line coverage {percentage:.2f}% is below threshold {threshold:.2f}%", file=sys.stderr)
            sys.exit(1)
        else:
            print(f"Line coverage {percentage:.2f}% meets or exceeds threshold {threshold:.2f}%")
            sys.exit(0)

    except FileNotFoundError:
        print(f"Error: Coverage XML file not found at {xml_file}", file=sys.stderr)
        sys.exit(1)
    except Exception as e:
        print(f"Error parsing XML or checking coverage: {e}", file=sys.stderr)
        sys.exit(1)

if __name__ == "__main__":
    if len(sys.argv) != 3:
        print("Usage: python check_coverage.py <coverage_xml_file> <threshold_percentage>", file=sys.stderr)
        sys.exit(1)
    
    xml_file_path = sys.argv[1]
    coverage_threshold = float(sys.argv[2])
    
    check_coverage(xml_file_path, coverage_threshold)