import os
import sys
import time
import requests

def main():
    api_url = 'https://app-staging.apiiro.com'
    token = os.getenv('APIIRO_TOKEN')
    build_id = os.getenv('BUILD_ID')
    
    if not token:
        print("Error: Missing APIIRO_TOKEN environment variable")
        return 1
    
    if not build_id:
        print("Error: Missing BUILD_ID environment variable")
        return 1
    
    print(f"Polling scan results for Build ID: {build_id}")
    
    # Set up headers
    headers = {
        'Authorization': f'Bearer {token}'
    }
    
    # Polling configuration
    max_wait_minutes = 15
    poll_interval_seconds = 30
    max_attempts = (max_wait_minutes * 60) // poll_interval_seconds
    
    print(f"Will poll every {poll_interval_seconds} seconds for up to {max_wait_minutes} minutes")
    
    for attempt in range(1, max_attempts + 1):
        print(f"Checking scan status... (Attempt {attempt}/{max_attempts})")
        
        try:
            response = requests.get(
                f"{api_url}/rest-api/v2/buildScan/{build_id}/results",
                headers=headers,
                timeout=30
            )
            
            if response.status_code == 200:
                print("Scan completed!")
                
                # Parse the JSON response
                try:
                    print(response.text)
                    scan_results = response.json()
                    build_passed = scan_results.get('buildPassed')
                    
                    if build_passed is None:
                        print("Error: buildPassed field not found in response")
                        return 1
                    
                    # Save results as artifact
                    try:
                        import json
                        with open('build_scan_results.json', 'w') as f:
                            json.dump(scan_results, f, indent=2)
                        print("Scan results saved to build_scan_results.json")
                    except Exception as e:
                        print(f"Warning: Could not save results file: {e}")
                    
                    if build_passed:
                        print("BUILD PASSED - No blocking issues found")
                        return 0
                    else:
                        print("BUILD FAILED - Blocking issues found")
                        return 1
                        
                except Exception as e:
                    print(f"Error parsing response: {e}")
                    return 1
                    
            elif response.status_code == 503:
                print("Scan still in progress...")
                if attempt < max_attempts:
                    time.sleep(poll_interval_seconds)
                    continue
                else:
                    print("Scan did not complete within timeout")
                    return 1
                    
            else:
                print(f"API request failed with status {response.status_code}: {response.text}")
                return 1
                
        except Exception as e:
            print(f"Request error: {e}")
            return 1
    
    print("Scan did not complete within the maximum wait time")
    return 1

if __name__ == '__main__':
    sys.exit(main())