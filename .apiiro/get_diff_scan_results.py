import os
import sys
import time
import requests
import json

def main():
    api_url = 'https://app-staging.apiiro.com'
    token = os.getenv('APIIRO_TOKEN')
    job_id = os.getenv('JOB_ID')
    
    if not token:
        print("Error: Missing APIIRO_TOKEN environment variable")
        return 1
    
    if not job_id:
        print("Error: Missing JOB_ID environment variable")
        return 1
    
    print(f"Polling diff scan results for Job ID: {job_id}")
    
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
                f"{api_url}/rest-api/v1/diffScans/{job_id}",
                headers=headers,
                timeout=30
            )
            
            if response.status_code == 200:
                print("Diff scan completed!")
                
                # Parse the JSON response
                try:
                    print(response.text)
                    scan_results = response.json()
                    
                    # Check the result field - if it's "Block", the scan failed
                    result = scan_results.get('result')
                    scan_passed = result != 'Block'
                    
                    # Save results as artifact
                    try:
                        import json
                        with open('diff_scan_results.json', 'w') as f:
                            json.dump(scan_results, f, indent=2)
                        print("Diff scan results saved to diff_scan_results.json")
                    except Exception as e:
                        print(f"Warning: Could not save results file: {e}")
                    
                    # Print summary information
                    if 'summary' in scan_results:
                        print(f"Scan Summary:\n{scan_results['summary']}")
                    
                    if 'scanUrl' in scan_results:
                        print(f"View detailed results: {scan_results['scanUrl']}")
                    
                    if scan_passed:
                        print(f"DIFF SCAN PASSED - Result: {result}")
                        return 0
                    else:
                        print(f"DIFF SCAN FAILED - Result: {result}")
                        if 'materialChanges' in scan_results:
                            blocking_changes = [change for change in scan_results['materialChanges'] 
                                              if change.get('action') == 'Block' and change.get('count', 0) > 0]
                            if blocking_changes:
                                print("Blocking issues found:")
                                for change in blocking_changes:
                                    print(f"  - {change.get('label', 'Unknown')}: {change.get('count', 0)} issues")
                        
                        return 1
                        
                except Exception as e:
                    print(f"Error parsing response: {e}")
                    return 1
                    
            elif response.status_code == 503:
                print("Diff scan still in progress...")
                if attempt < max_attempts:
                    time.sleep(poll_interval_seconds)
                    continue
                else:
                    print("Diff scan did not complete within timeout")
                    return 1
                    
            elif response.status_code == 404:
                print("Diff scan job not found - it may not have started yet...")
                if attempt < max_attempts:
                    time.sleep(poll_interval_seconds)
                    continue
                else:
                    print("Diff scan job was never found")
                    return 1
                    
            else:
                print(f"API request failed with status {response.status_code}: {response.text}")
                return 1
                
        except Exception as e:
            print(f"Request error: {e}")
            return 1
    
    print("Diff scan did not complete within the maximum wait time")
    return 1

if __name__ == '__main__':
    sys.exit(main())