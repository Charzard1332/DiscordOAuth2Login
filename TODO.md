# 🚀 TODO - Future Enhancements

This document lists **planned features** and improvements for the **DiscordOAuth2Login** project.

---

## 🔥 High Priority Features
### 🎮 Discord API Enhancements
- [x] **Check for Boosted Servers** (Detect if the user has boosted any servers)
- [x] **Token Refresh Mechanism** (Avoid manual relogins)
- [x] **Retrieve User's Discord Badges** (Partner, Hypesquad, etc.)
- [x] **Retrieve User's Connected Accounts** (Steam, Xbox, Spotify, etc.)
- [ ] **Fetch User's Avatar & Status** (Display profile picture & online status)
- [ ] **Retrieve User's Active Discord Sessions** (Check logged-in devices)
- [ ] **Fetch User's Server Roles** (List user roles per server)

### 🖥️ UI & Experience
- [ ] **GUI Version** (WPF/WinForms UI for easy usage)
- [ ] **Better Console Output** (Color-coded and formatted response)
- [ ] **Configurable Settings File** (`appsettings.json` for storing credentials)
- [ ] **Add Icons for Badges, Nitro Status & Connected Accounts** (Improve visual clarity)
- [ ] **Export User Data as JSON or CSV** (Allow users to save their profile info)

### 🌎 Multi-Platform Support
- [ ] **Docker Support** (Run the app inside a Docker container)
- [ ] **Linux & MacOS Compatibility** (Ensure it runs smoothly)
- [ ] **Cross-Platform GUI Version** (Electron or MAUI for broader support)

---

## 🛠️ Medium Priority Enhancements
### 🔗 API & Backend Improvements
- [ ] **Better Error Handling** (Gracefully handle API failures)
- [ ] **Rate Limit Handling** (Respect Discord API limits)
- [ ] **Add Logging System** (Log API calls and responses)
- [ ] **Support for Multiple Discord Accounts** (Switch accounts seamlessly)
- [ ] **Filter Connected Accounts by Type** (Show only gaming platforms, music, etc.)

### 🛡️ Security Improvements
- [ ] **Encrypt API Credentials** (Avoid exposing secrets in `Program.cs`)
- [ ] **Use Environment Variables** for storing credentials
- [ ] **Encrypt `tokens.json` file** (Prevent unauthorized access to saved tokens)
- [ ] **Secure OAuth2 Redirection** (Prevent token leakage)

### 🚀 Performance Optimizations
- [ ] **Reduce API Calls** (Cache results where possible)
- [ ] **Async Performance Tweaks** (Improve response time)
- [ ] **Use Database Storage for Tokens & User Data** (Avoid reliance on JSON files)

---

## 🤝 Collaboration & Community Features
### 👥 Improve Contribution Workflow
- [ ] **Write Unit Tests** (Ensure code reliability)
- [ ] **Create GitHub Wiki** (Better documentation for contributors)
- [ ] **Enable GitHub Discussions** (Allow users to discuss features)
- [ ] **Automate Builds & Tests** (Use GitHub Actions)
- [ ] **Add Localization Support** (Multiple language support)
- [ ] **Create a Discord Bot Version** (Allow bot commands to fetch user info)

---

📢 **Want to suggest a feature?**  
- Open a **GitHub Issue** with the `[Feature Request]` tag.
- Or **join discussions** on the repo.

🚀 **Let’s build this together!**  
💙 **Contribute, star, and share!**
