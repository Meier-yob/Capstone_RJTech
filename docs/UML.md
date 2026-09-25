# Data Model (UML)

Mermaid `classDiagram` for the live editor at https://mermaid.live — classes from the
`Models` folder only. Every class shows the three compartments: **Name**, **Attributes**,
and **Operations**.

> Operations = the controller/service actions that manage that record, plus the
> `[NotMapped]` computed getters defined on the class (shown with `()`).
> The 16 report snapshot entities were removed in the report refactor; reports are now
> computed live at runtime from the business tables below.
>
> Relationships: `*--` (composition) marks records that **cannot exist without their
> parent** and are cascade-deleted with it (Delivery→DeliveryDetails, Checkout→CheckoutItem,
> Checkout→Installment, Installment→InstallmentPayment). `-->` (association) links records
> that live independently.

```mermaid
classDiagram
    direction TB

    class AppUser {
        <<tblUser>>
        +string UserID $PK
        +string FullName
        +string Email $UQ
        +string Username $UQ
        +string Password
        +string Role
        +DateTime DateCreated
        +SignUp()
        +Login()
        +Logout()
        +ForgotPassword()
        +ResetPassword()
        +ChangePassword()
        +UpdateProfile()
    }

    class PasswordResetCode {
        <<tblPasswordResetCode>>
        +string Id $PK
        +string Email
        +string OtpHash
        +DateTimeOffset ExpiresAt
        +DateTimeOffset? VerifiedAt
        +CreateCodeAsync()
        +GetPendingRequestAsync()
        +VerifyCodeAsync()
        +GetVerifiedRequestAsync()
        +ResetPasswordAsync()
    }

    class ProductCategory {
        <<ProductCategories>>
        +int category_ID $PK
        +string category_name $UQ
        +CreateCategory()
        +EditCategory()
        +GetNextCategoryCode()
    }

    class Product {
        <<Products>>
        +int product_ID $PK
        +string product_name
        +string product_brand
        +string? product_description
        +byte[]? product_Image
        +string? product_ImageContentType
        +int product_quantity
        +int reorder_level
        +decimal Product_price
        +bool is_serialized
        +string product_status
        +int category_ID $FK
        +Create()
        +BulkCreate()
        +UpdateProductDetails()
        +UploadImage()
        +DeleteProduct()
        +SearchProducts()
        +GetDetails()
        +string formatted_code()
        +string product_code()
    }

    class Delivery {
        <<Deliveries>>
        +int delivery_ID $PK
        +DateTime date_delivered
        +string received_by
        +string batch_ID $UQ
        +bool is_archived
        +CompleteDelivery()
        +DeleteDeliveryReceipt()
        +GetDeliveries()
        +string FormattedDeliveryID()
        +string FormattedBatchID()
    }

    class DeliveryDetails {
        <<DeliveryDetails>>
        +int deldetails_ID $PK
        +int product_quantity
        +int previous_quantity
        +int new_quantity
        +int product_ID $FK
        +int delivery_ID $FK
        +ReceiveProduct()
        +SearchProductForDelivery()
    }

    class AppNotification {
        <<Notifications>>
        +int notification_ID $PK
        +int? product_ID $FK
        +string title
        +string message
        +string notification_type
        +string action_url
        +DateTime created_at
        +bool is_read
        +GetNotifications()
        +MarkAsRead()
        +MarkAllAsRead()
    }

    class Customer {
        <<tblCustomer>>
        +int customer_ID $PK
        +string customer_FullName
        +string customer_Email $UQ
        +string customer_Phone
        +string customer_Address
        +SearchCustomers()
        +DeleteCustomers()
    }

    class Checkout {
        <<tblCheckout>>
        +int CheckoutID $PK
        +int CustomerID $FK
        +decimal TotalAmount
        +string PaymentMethod
        +string PaymentType
        +DateTime DatePurchased
        +string Status
        +CompleteCheckout()
        +RefundCheckout()
        +DeleteCheckout()
        +SelectedCheckoutDetails()
        +string FormattedCheckoutID()
    }

    class CheckoutItem {
        <<tblCheckoutItem>>
        +int CheckoutItemID $PK
        +int CheckoutID $FK
        +int ProductID $FK
        +string? SerialNo
        +int ItemQuantity
        +decimal Price
        +decimal SubTotal
        +CheckSerialNumber()
    }

    class Installment {
        <<tblInstallment>>
        +int InstallmentID $PK
        +int CheckoutID $FK
        +int Months
        +decimal DownPayment
        +decimal InterestRate
        +decimal TotalAmount
        +decimal Balance
        +decimal MonthlyPayment
        +int MonthsPaid
        +int MonthsRemaining
        +DateTime StartDate
        +string Status
        +RecordPayment()
        +DeleteInstallments()
        +GetDetails()
        +CalculateMonthsPaid()
        +CalculateDisplayDate()
        +CalculateCompletionDate()
        +DetermineInstallmentStatus()
        +string FormattedInstallmentID()
    }

    class InstallmentPayment {
        <<tblInstallmentPayment>>
        +int PaymentID $PK
        +int InstallmentID $FK
        +string PaymentMethod
        +decimal PaymentAmount
        +DateTime PaymentDate
        +string Status
        +RecordPayment()
    }

    class CustomerPurchaseHistory {
        <<tblCustomerPurchaseHistory>>
        +int HistoryID $PK
        +int CustomerID $FK
        +int CheckoutID $FK
        +DateTime PurchaseDate
        +decimal TotalAmount
        +string PaymentMethod
        +RecordInstallmentPayment()
        +string FormattedHistoryID()
    }

    ProductCategory "1" --> "m" Product : has
    Product "1" --> "m" DeliveryDetails : restocked in
    Product "1" --> "m" CheckoutItem : sold in
    Product "1" --> "0..1" AppNotification : triggers
    Delivery "1" *-- "m" DeliveryDetails : contains
    Customer "1" --> "m" Checkout : places
    Checkout "1" *-- "m" CheckoutItem : contains
    Checkout "1" *-- "0..1" Installment : financed by
    Installment "1" *-- "m" InstallmentPayment : receives
    Customer "1" --> "m" CustomerPurchaseHistory : purchase history
    Checkout "1" --> "m" CustomerPurchaseHistory : payment history
```