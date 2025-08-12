import os
import sys
import requests

def main():
    # Get required environment variables
    api_url = 'https://app-staging.apiiro.com'
    token = os.getenv('APIIRO_TOKEN')
    repo_url = os.getenv('GITHUB_REPOSITORY_URL')
    current_branch = os.getenv('CURRENT_BRANCH')
    default_branch = os.getenv('DEFAULT_BRANCH')
    
    print("Triggering diff scan...")
    print(f"Repository: {repo_url}")
    print(f"Candidate branch: {current_branch}")
    print(f"Baseline branch: {default_branch}")
    
    # API payload based on the structure shown in the image
    payload = {
        "repositoryUrl": repo_url,
        "candidate": {
            "commitish": current_branch,
            "type": "Branch"
        },
        "baseline": {
            "commitish": default_branch,
            "type": "Branch"
        }
    }
    
    # Make API call
    headers = {
        'Authorization': f'Bearer {token}',
        'Content-Type': 'application/json'
    }
    
    try:
        response = requests.post(
            f"{api_url}/rest-api/v1/diffScans",
            json=payload,
            headers=headers,
        )
        
        if response.status_code == 200:
            print("Diff scan triggered successfully!")
            
            job_id = response.text.strip()
            
            if not job_id:
                print("Error: API did not return jobId")
                print(f"Response: {response.text}")
                return 1
            
            print(f"Job ID: {job_id}")
            
            # Output the jobId as a GitHub Actions output
            github_output = os.getenv('GITHUB_OUTPUT')
            if github_output:
                with open(github_output, 'a') as f:
                    f.write(f"job_id={job_id}\n")
                print("Job ID set as GitHub Actions output")
            
            return 0
        else:
            print(f"API request failed with status {response.status_code}: {response.text}")
            return 1
            
    except Exception as e:
        print(f"Request error: {e}")
        return 1

if __name__ == '__main__':
    sys.exit(main())