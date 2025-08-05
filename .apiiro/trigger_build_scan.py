import os
import sys
import requests

def main():
    # Get required environment variables
    api_url = 'https://app-staging.apiiro.com'
    token = os.getenv('APIIRO_TOKEN')
    commit_sha = os.getenv('GITHUB_SHA')
    repo_url = os.getenv('GITHUB_REPOSITORY_URL')
    
    print("Triggering scan...")
    print(f"Commit: {commit_sha[:8]}...")
    print(f"Repository: {repo_url}")
    
    # API payload - minimal
    payload = {
        "commitSha": commit_sha,
        "repositoryUrl": repo_url
    }
    
    # Make API call
    headers = {
        'Authorization': f'Bearer {token}',
        'Content-Type': 'application/json-patch+json'
    }
    
    try:
        response = requests.post(
            f"{api_url}/rest-api/v1/buildScan/builds",
            json=payload,
            headers=headers,
        )
        
        if response.status_code == 200:
            print("Scan triggered successfully!")
            
            # The API returns the buildId directly as plain text
            returned_build_id = response.text.strip()
            
            if not returned_build_id:
                print("Error: API returned empty buildId")
                return 1
            
            print(f"Build ID: {returned_build_id}")
            
            # Output the buildId as a GitHub Actions output
            github_output = os.getenv('GITHUB_OUTPUT')
            if github_output:
                with open(github_output, 'a') as f:
                    f.write(f"build_id={returned_build_id}\n")
                print("Build ID set as GitHub Actions output")
            
            return 0
        else:
            print(f"API request failed with status {response.status_code}: {response.text}")
            return 1
            
    except Exception as e:
        print(f"Request error: {e}")
        return 1

if __name__ == '__main__':
    sys.exit(main())