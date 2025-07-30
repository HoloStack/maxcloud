
import subprocess
import sys
import os
from datetime import datetime

import platform

def check_azure_cli():
    """Checks if Azure CLI is installed and configured. Installs it if missing."""
    try:
        subprocess.run(["az", "--version"], check=True, capture_output=True)
        print("Azure CLI is installed.")
    except (subprocess.CalledProcessError, FileNotFoundError):
        print("Azure CLI is not installed. Attempting installation...")
        user_platform = platform.system().lower()
        if user_platform == "darwin":
            # macOS
            print("Installing Azure CLI via Homebrew (macOS)...")
            try:
                subprocess.run(["brew", "install", "azure-cli"], check=True)
            except Exception as e:
                print(f"Homebrew install failed: {e}")
                print("Please manually install Azure CLI from https://docs.microsoft.com/cli/azure/install-azure-cli")
                return False
        elif user_platform == "windows":
            print("Installing Azure CLI on Windows. This requires admin rights and winget.")
            try:
                subprocess.run(["winget", "install", "Microsoft.AzureCLI"], check=True)
            except Exception as e:
                print(f"winget install failed: {e}")
                print("Please manually install Azure CLI from https://docs.microsoft.com/cli/azure/install-azure-cli")
                return False
        else:
            print(f"Your OS ({user_platform}) is not directly supported by this script for auto-install. Please install Azure CLI manually.")
            return False
        print("Azure CLI installed. Please restart your terminal and re-run this script.")
        sys.exit(0)
    # Check login
    try:
        account_info = subprocess.run(["az", "account", "show"], capture_output=True, text=True)
        if "Please run 'az login' to setup account." in account_info.stderr or account_info.returncode != 0:
            print("You are not logged into Azure CLI. Please complete authentication in your browser...")
            subprocess.run(["az", "login"], check=True)
            print("Login successful.")
        return True
    except Exception as e:
        print(f"Error during Azure login: {e}")
        return False

def get_blob_storage_data():
    """Gets data from Azure Blob Storage."""
    print("\n--- Azure Blob Storage ---")
    connection_string = input("Enter the Azure Storage connection string: ")
    container_name = input("Enter the container name: ")

    with open("proof.txt", "a") as f:
        f.write(f"## Azure Blob Storage - Container: {container_name}\n")
        f.write(f"Timestamp: {datetime.now()}\n\n")

    try:
        # Using Azure CLI to list blobs
        command = [
            "az", "storage", "blob", "list",
            "--container-name", container_name,
            "--connection-string", connection_string,
            "--output", "table"
        ]
        result = subprocess.run(command, check=True, capture_output=True, text=True)
        with open("proof.txt", "a") as f:
            f.write(result.stdout)
        print(f"Successfully wrote blob storage data to proof.txt")

    except subprocess.CalledProcessError as e:
        print(f"Error fetching blob storage data: {e.stderr}")
    except FileNotFoundError:
        print("Error: 'az' command not found. Make sure Azure CLI is installed.")


def get_table_storage_data():
    """Gets data from Azure Table Storage."""
    print("\n--- Azure Table Storage ---")
    connection_string = input("Enter the Azure Storage connection string: ")
    table_name = input("Enter the table name: ")

    with open("proof.txt", "a") as f:
        f.write(f"## Azure Table Storage - Table: {table_name}\n")
        f.write(f"Timestamp: {datetime.now()}\n\n")

    try:
        command = [
            "az", "storage", "entity", "query",
            "--table-name", table_name,
            "--connection-string", connection_string,
            "--output", "table"
        ]
        result = subprocess.run(command, check=True, capture_output=True, text=True)
        with open("proof.txt", "a") as f:
            f.write(result.stdout)
        print(f"Successfully wrote table storage data to proof.txt")

    except subprocess.CalledProcessError as e:
        print(f"Error fetching table storage data: {e.stderr}")
    except FileNotFoundError:
        print("Error: 'az' command not found. Make sure Azure CLI is installed.")


def get_database_data():
    """Gets data from a generic database."""
    print("\n--- Generic Database ---")
    print("This function is a placeholder. You need to implement the connection")
    print("and query logic for your specific database (e.g., SQL Server, MySQL, PostgreSQL).")
    
  

def main():
    """Main function to drive the script."""
    if os.path.exists("proof.txt"):
        os.remove("proof.txt")

    choice = input(
        "Select the data source for the proof document:\n"
        "1. Azure Blob Storage\n"
        "2. Azure Table Storage\n"
        "3. Generic Database\n"
        "Enter your choice (1, 2, or 3): "
    )

    if choice == '1':
        if check_azure_cli():
            get_blob_storage_data()
    elif choice == '2':
        if check_azure_cli():
            get_table_storage_data()
    elif choice == '3':
        get_database_data()
    else:
        print("Invalid choice. Please run the script again and select 1, 2, or 3.")

if __name__ == "__main__":
    main()

