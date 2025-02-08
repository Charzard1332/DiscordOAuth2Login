# 🎮 Discord OAuth2 Login - .NET Console App

🚀 A **.NET Core console application** that allows users to **log in via Discord OAuth2** and **check their Nitro status**.

---

## ✅ Features
- ✅ **Login via Discord OAuth2**
- ✅ **Fetch and display Discord username & Nitro status**
- ✅ **Check if the user has boosted any servers**
- ✅ **Uses `HttpClient` for API requests**
- ✅ **Simple, lightweight, and open-source**

---

## ⚙️ Setup & Installation

### 🔹 **1️⃣ Clone the Repository**
```sh
git clone https://github.com/Charzard1332/DiscordOAuth2Login.git
cd DiscordOAuth2Login
```

### 🔹 **2️⃣ Install .NET SDK**
Ensure you have the **.NET SDK** installed on your machine. If you don't have it, download it from:
[.NET Download](https://dotnet.microsoft.com/download/dotnet)

To verify installation, run:
```sh
dotnet --version
```

### 🔹 **3️⃣ Configure Your Discord Application**
1. Go to the [Discord Developer Portal](https://discord.com/developers/applications)
2. Create a new application
3. Navigate to **OAuth2** → **General**
4. Note down your **Client ID** and **Client Secret**
5. Set a **Redirect URI** to: `http://localhost:5000/callback`

### 🔹 **4️⃣ Set Up Environment Variables**
Create a `.env` file in the project root and add:
```sh
DISCORD_CLIENT_ID=your_client_id
DISCORD_CLIENT_SECRET=your_client_secret
DISCORD_REDIRECT_URI=http://localhost:5000/callback
```

Alternatively, you can set them manually in your shell:
```sh
export DISCORD_CLIENT_ID=your_client_id
export DISCORD_CLIENT_SECRET=your_client_secret
export DISCORD_REDIRECT_URI=http://localhost:5000/callback
```

### 🔹 **5️⃣ Run the Application**
Run the following command to start the application:
```sh
dotnet run
```

---

## 🛠 Usage
- Open the provided **OAuth2 login URL** in your browser.
- Authorize the application using your Discord account.
- The app will fetch and display your **Discord username and Nitro status**.

---

## 📝 License
This project is open-source and available under the **MIT License**.

---

## 🌟 Contributing
Feel free to submit **issues** or **pull requests** to improve the project!

---

## 📌 Disclaimer
This project is for **educational purposes** and is **not affiliated with Discord** in any way.
