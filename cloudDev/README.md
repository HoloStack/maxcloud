# maxStore - ASP.NET Core E-commerce Web Application

This is a feature-rich e-commerce web application built with ASP.NET Core, designed to provide a seamless shopping experience for users and a powerful management interface for administrators.

## Key Features:

### User Experience:
- **Product Catalog**: Browse and search a wide range of products with images and descriptions.
- **Shopping Cart**: Add, remove, and update items in the cart.
- **Secure Checkout**: Complete purchases securely with payment integration.
- **User Accounts**: Register, log in, and manage personal information.

### Admin Dashboard:
- **Item Management**: Add, edit, and delete products.
- **Stock Control**: Update product quantities and manage inventory.
- **Sales Reporting**: View detailed sales reports by date, item, or customer.
- **Media Management**: Upload and manage product images and videos.

### Technology Stack:
- **Backend**: ASP.NET Core, C#
- **Frontend**: Razor Pages, HTML, CSS, JavaScript
- **Database**: Azure Table Storage
- **File Storage**: Azure Blob Storage
- **Authentication**: ASP.NET Core Identity, Google OAuth

## How to Run:

1. **Clone the repository**:
   ```bash
   git clone https://github.com/HoloStack/maxStore.git
   ```

2. **Configure Azure services**:
   - Set up your Azure Storage Account (Blob and Table Storage).
   - Update the connection strings in `appsettings.json`.

3. **Run the application**:
   - Open the solution in Visual Studio or use the .NET CLI:
     ```bash
     dotnet run
     ```

## Repository Structure:

- **/Controllers**: Handles incoming HTTP requests and business logic.
- **/Models**: Defines the data structures for items, users, and sales.
- **/Views**: Contains the Razor templates for the user interface.
- **/Services**: Implements business logic for Azure services and database operations.
- **/wwwroot**: Stores static assets like CSS, JavaScript, and images.



