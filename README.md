# ⚖️ Legal Office Notary Management System

> A modern desktop-based Legal Office Notary Management System developed using **C# WinForms (.NET 8)** with **Google Firebase Firestore**, **MEGA Cloud Storage**, and **Gmail SMTP** integration.

---

## 📖 Overview

The **Legal Office Notary Management System** is a centralized desktop application designed to streamline the submission, management, and tracking of notarial records within a legal office. The system replaces traditional paper-based workflows with a secure digital platform that enables office personnel to efficiently manage documents, monitor submission status, and maintain organized activity logs.

Developed as a **SPECIAL PROGRAM FOR EMPLOYMENT OF STUDENTS (SPES)** program endorsement project, this application demonstrates the integration of modern cloud technologies into a traditional desktop environment while emphasizing usability, accessibility, and maintainability.

---

## ✨ Key Features

* 🔐 Secure user authentication with role-based access
* 👥 Separate Administrator and Staff permissions
* 📤 Digital notarial book submission
* 📊 Interactive dashboard for office monitoring
* 📄 Submission tracking and status updates
* 📝 Comprehensive activity logging
* ☁️ Cloud-based data storage using Firebase Firestore
* 📁 Secure document storage through MEGA Cloud
* 📧 Email notification support using Gmail SMTP
* 🖥️ Clean and responsive WinForms user interface

---

## 🛠️ Technologies Used

### Frontend

* C#
* Windows Forms (.NET 8)

### Backend & Cloud

* Google Firebase Firestore
* MEGA Cloud Storage

### Communication

* MailKit
* MimeKit
* Gmail SMTP

### Development Environment

* Visual Studio Code
* .NET SDK 8
* Git
* GitHub

---

## 📂 Project Structure

```text
LegalOfficeApp/
│
├── Controls/
├── Forms/
├── Models/
├── Services/
├── Resources/
│
├── Program.cs
├── SessionManager.cs
└── LegalOfficeApp.csproj
```

---

## 🔑 System Modules

### Dashboard

Provides an overview of office activities, document statistics, and system information.

### Notarial Submission

Allows office staff to submit digital records and supporting documents.

### Submission Tracking

Tracks document progress throughout the notarial process.

### Activity Logs

Maintains a history of important user activities for accountability and auditing.

### User Management

Enables administrators to manage user accounts, roles, and access permissions.

---

## ☁️ Cloud Architecture

The application utilizes a hybrid cloud architecture:

* **Firebase Firestore** serves as the centralized database for user accounts, submissions, activity logs, and system configuration.
* **MEGA Cloud Storage** securely stores uploaded documents and attachments.
* **Gmail SMTP** handles automated email notifications and communication.

This architecture allows multiple office workstations to securely access synchronized data without relying on local storage.

---

## 🚀 Getting Started

### Prerequisites

* .NET 8 SDK
* Google Firebase Project
* Firebase Service Account Credentials
* MEGA Account
* Gmail Account with App Password enabled

### Installation

```bash
git clone https://github.com/nom0mon/claudeNotaryApp.git
```

Navigate to the project directory.

```bash
cd claudeNotaryApp
```

Restore dependencies.

```bash
dotnet restore
```

Run the application.

```bash
dotnet run
```

---

## ⚙️ Configuration

Before running the application, configure the following services:

* Firebase Firestore
* Firebase Service Account Credentials
* MEGA Cloud Storage
* Gmail SMTP

Configuration files should **never** be committed to the repository.

---

## 📌 Current Development Status

This project is currently under active development.

Planned improvements include:

* Enhanced dashboard analytics
* Document approval workflow
* PDF generation
* Search and filtering improvements
* Backup and recovery features
* User profile management
* Additional reporting capabilities

---

## 🎓 Academic Purpose

This project was developed as part of the summer job endorsement project for the **SPECIAL PROGRAM FOR EMPLOYMENT OF STUDENTS (SPES)** program. It serves as a practical implementation of desktop application development, cloud database integration, file storage management, and secure user authentication within a real-world office environment.

---

## 🤝 Contributions

Suggestions, improvements, and constructive feedback are welcome. Feel free to fork the repository, submit pull requests, or open issues to help improve the project.

---

## 📄 License

This repository is intended for educational, practical, and research purposes. Please contact the repository owner before using substantial portions of the project in production environments.

---

## 👨‍💻 Developer

**Gian Simon Lopez**

Bachelor of Science in **Information Technology**

*"Building Better Balance."*
