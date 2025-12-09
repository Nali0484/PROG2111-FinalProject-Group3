-- Group 3 - Phase 4: DDL Statements --

-- Create and use database
CREATE DATABASE IF NOT EXISTS BookBuster;
USE BookBuster;

-- First: Create independent tables (no foreign keys)
CREATE TABLE IF NOT EXISTS Provinces(
    ProvinceID INT PRIMARY KEY NOT NULL AUTO_INCREMENT,
    Province VARCHAR(50) NOT NULL UNIQUE
);

CREATE TABLE IF NOT EXISTS Cities(
    CityID INT PRIMARY KEY AUTO_INCREMENT,
    City VARCHAR(50) NOT NULL,
    ProvinceID INT NOT NULL,
    CONSTRAINT FOREIGN KEY fk_Province (ProvinceID) REFERENCES Provinces(ProvinceID)
);

CREATE TABLE IF NOT EXISTS Addresses(
    AddressID INT PRIMARY KEY AUTO_INCREMENT,
    Street VARCHAR(50) NOT NULL,
    CityID INT NOT NULL,
    CONSTRAINT FOREIGN KEY fk_city (CityID) REFERENCES Cities(CityID)
);

-- Now create Books and related tables
CREATE TABLE IF NOT EXISTS Books(
    BookID INT PRIMARY KEY AUTO_INCREMENT,
    Title VARCHAR(50) NOT NULL,
    Author VARCHAR(30) NOT NULL,
    PublishingYear YEAR NOT NULL,
    BookLanguage VARCHAR(15) NOT NULL,
    RentalStatus ENUM('Rented', 'Available') NOT NULL DEFAULT 'Available'
);

CREATE TABLE IF NOT EXISTS Genres(
    GenreID INT PRIMARY KEY AUTO_INCREMENT,
    Genre VARCHAR(20) NOT NULL UNIQUE
);

CREATE TABLE IF NOT EXISTS BookGenre(
    BookID INT NOT NULL,
    GenreID INT NOT NULL,
    PRIMARY KEY(BookID, GenreID),
    FOREIGN KEY (BookID) REFERENCES Books(BookID) ON DELETE CASCADE,
    FOREIGN KEY (GenreID) REFERENCES Genres(GenreID) ON DELETE CASCADE
);

-- Now create Staff and Roles tables
CREATE TABLE IF NOT EXISTS Staff(
    StaffID INT PRIMARY KEY AUTO_INCREMENT,
    FirstName VARCHAR(15) NOT NULL,
    LastName VARCHAR(15) NOT NULL,
    Email VARCHAR(50) UNIQUE,
    PhoneNumber VARCHAR(20) UNIQUE
);

CREATE TABLE IF NOT EXISTS Roles(
    RoleID INT PRIMARY KEY AUTO_INCREMENT,
    RoleName VARCHAR(15) NOT NULL UNIQUE
);

CREATE TABLE IF NOT EXISTS StaffRole(
    StaffID INT NOT NULL,
    RoleID INT NOT NULL,
    PRIMARY KEY(StaffID, RoleID),
    FOREIGN KEY (StaffID) REFERENCES Staff(StaffID) ON DELETE CASCADE,
    FOREIGN KEY (RoleID) REFERENCES Roles(RoleID) ON DELETE CASCADE
);

-- Now create Members (depends on Addresses)
CREATE TABLE IF NOT EXISTS Members(
    MemberID INT PRIMARY KEY AUTO_INCREMENT,
    FirstName VARCHAR(15) NOT NULL,
    LastName VARCHAR(15) NOT NULL,
    JoinDate DATE NOT NULL,
    Email VARCHAR(50) UNIQUE,
    PhoneNumber VARCHAR(20) UNIQUE,
    AddressID INT NOT NULL,
    CONSTRAINT FOREIGN KEY fk_address (AddressID) REFERENCES Addresses (AddressID)
);

-- Now create Loans (depends on Books, Members, and Staff)
CREATE TABLE IF NOT EXISTS Loans(
    LoanID INT PRIMARY KEY AUTO_INCREMENT,
    MemberID INT NOT NULL,
    BookID INT NOT NULL,
    StaffID INT NOT NULL,
    CheckoutDate DATE NOT NULL,
    DueDate DATE NOT NULL,
    ReturnDate DATE,
    CONSTRAINT FOREIGN KEY fk_Book (BookID) REFERENCES Books (BookID),
    CONSTRAINT FOREIGN KEY fk_Member (MemberID) REFERENCES Members (MemberID),
    CONSTRAINT FOREIGN KEY fk_Staff (StaffID) REFERENCES Staff (StaffID)
);

-- Now create Fines (depends on Loans)
CREATE TABLE IF NOT EXISTS Fines(
    FineID INT PRIMARY KEY AUTO_INCREMENT,
    LoanID INT NOT NULL UNIQUE,
    FOREIGN KEY (LoanID) REFERENCES Loans(LoanID) ON DELETE CASCADE
);

-- Now create Payments (depends on Fines) 
CREATE TABLE IF NOT EXISTS Payments(
    PaymentID INT PRIMARY KEY AUTO_INCREMENT,
    FineID INT NOT NULL,
    AmountPaid DECIMAL(5,2) NOT NULL,  
    PaymentDate DATE NOT NULL,
    PaymentMethod VARCHAR(20) NOT NULL,
    FOREIGN KEY (FineID) REFERENCES Fines(FineID) ON DELETE CASCADE
);

-- Create Views
-- Drop existing views if they exist
DROP VIEW IF EXISTS LoanFineDetails;
DROP VIEW IF EXISTS MemberBalances;

-- Create LoanFineDetails view with COALESCE to handle NULL values
CREATE VIEW LoanFineDetails AS
SELECT
    l.LoanID,
    l.MemberID,
    l.CheckoutDate,
    l.ReturnDate,
    l.DueDate,
    GREATEST(DATEDIFF(l.ReturnDate, l.DueDate), 0) AS DaysLate,
    GREATEST(DATEDIFF(l.ReturnDate, l.DueDate), 0) AS FineAmount,
    f.FineID,
    l.ReturnDate AS FineAssessedDate,
    COALESCE(SUM(p.AmountPaid), 0) AS TotalPaid,
    (GREATEST(DATEDIFF(l.ReturnDate, l.DueDate), 0) - COALESCE(SUM(p.AmountPaid), 0)) AS AmountOwing
FROM Loans l
LEFT JOIN Fines f ON f.LoanID = l.LoanID
LEFT JOIN Payments p ON p.FineID = f.FineID
WHERE l.ReturnDate IS NOT NULL
GROUP BY l.LoanID, l.MemberID, l.CheckoutDate, l.ReturnDate, l.DueDate, f.FineID
ORDER BY l.LoanID;

-- Create MemberBalances view 
CREATE VIEW MemberBalances AS
SELECT
    members.MemberID,
    CONCAT(members.FirstName, ' ', members.LastName) as MemberName,
    members.JoinDate,
    members.Email,
    members.PhoneNumber,
    a.AddressID,
    a.Street,
    c.City,
    p.Province,
    SUM(
        CASE 
            WHEN DATEDIFF(loan.ReturnDate, loan.DueDate) > 0 
            THEN DATEDIFF(loan.ReturnDate, loan.DueDate)
            ELSE 0
        END
    ) AS TotalFineAmount,
    COALESCE(SUM(pments.AmountPaid), 0) AS TotalPaid,
    COALESCE(
        SUM(
            CASE 
                WHEN DATEDIFF(loan.ReturnDate, loan.DueDate) > 0
                THEN DATEDIFF(loan.ReturnDate, loan.DueDate)
                ELSE 0
            END
        ) - SUM(pments.AmountPaid), 
        SUM(
            CASE 
                WHEN DATEDIFF(loan.ReturnDate, loan.DueDate) > 0
                THEN DATEDIFF(loan.ReturnDate, loan.DueDate)
                ELSE 0
            END
        )
    ) AS BalanceOwing
FROM Members members
LEFT JOIN Loans loan ON members.MemberID = loan.MemberID
LEFT JOIN Fines fine ON fine.LoanID = loan.LoanID
LEFT JOIN Payments pments ON fine.FineID = pments.FineID
JOIN Addresses a ON members.AddressID = a.AddressID
JOIN Cities c ON a.CityID = c.CityID
JOIN Provinces p ON c.ProvinceID = p.ProvinceID
WHERE loan.ReturnDate IS NOT NULL
GROUP BY members.MemberID, MemberName, members.JoinDate, members.Email, 
         members.PhoneNumber, a.AddressID, a.Street, c.City, p.Province
ORDER BY members.MemberID;