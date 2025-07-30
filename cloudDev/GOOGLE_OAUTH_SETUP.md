# Google OAuth Setup Guide

## 🚀 Setting up Google OAuth for Your ASP.NET Core App (100% FREE)

This guide will help you set up Google OAuth authentication for your application using only **FREE** Google Cloud services.

### Prerequisites
- Your Google Cloud project: `partstracker-app-1750857082`
- Google Cloud CLI (gcloud) installed and authenticated

### Step 1: Configure OAuth Consent Screen

1. **Open Google Cloud Console**: https://console.cloud.google.com/
2. **Select your project**: `partstracker-app-1750857082`
3. **Navigate to**: APIs & Services → OAuth consent screen
4. **Choose "External" user type** (This is FREE for up to 100 users)
5. **Fill in required fields**:
   - App name: `CloudDev Store`
   - User support email: `jethros9@gmail.com`
   - Developer contact email: `jethros9@gmail.com`
6. **Save and Continue**

### Step 2: Create OAuth 2.0 Credentials

1. **Navigate to**: APIs & Services → Credentials
2. **Click**: "Create Credentials" → "OAuth client ID"
3. **Select**: "Web application"
4. **Application name**: `CloudDev Store Web Client`
5. **Authorized redirect URIs**:
   - `https://localhost:7129/signin-google`
   - `http://localhost:5104/signin-google`
6. **Click "Create"**
7. **Copy the Client ID and Client Secret**

### Step 3: Update Your Application

Replace the placeholders in `appsettings.json`:

```json
{
  "Authentication": {
    "Google": {
      "ClientId": "YOUR_ACTUAL_CLIENT_ID_HERE",
      "ClientSecret": "YOUR_ACTUAL_CLIENT_SECRET_HERE"
    }
  }
}
```

### Step 4: Test Your Application

1. **Build and run**:
   ```bash
   dotnet build
   dotnet run --urls="https://localhost:7129"
   ```

2. **Navigate to**: https://localhost:7129/Account/Login
3. **Click**: "Sign in with Google"
4. **Test the OAuth flow**

### 🆓 Cost Breakdown

- **OAuth consent screen**: FREE
- **OAuth 2.0 credentials**: FREE
- **Google Sign-In API calls**: FREE (no quotas)
- **External user type**: FREE for up to 100 users

### Security Notes

- Store your Client Secret securely
- Never commit secrets to version control
- Consider using User Secrets for development:
  ```bash
  dotnet user-secrets set "Authentication:Google:ClientId" "your-client-id"
  dotnet user-secrets set "Authentication:Google:ClientSecret" "your-client-secret"
  ```

### Troubleshooting

- **Error 400**: Check your redirect URIs match exactly
- **Error 403**: Verify your OAuth consent screen is configured
- **User not found**: The app automatically creates new users from Google accounts

### What's Implemented

✅ Google OAuth 2.0 integration
✅ Automatic user registration
✅ Secure session management
✅ Fallback to traditional login
✅ Admin role preservation
✅ FREE tier compliant

Your Google OAuth setup is now complete! 🎉
