-- Group 3 - Phase 4: DDL Statements --

-- Create and use database
CREATE DATABASE IF NOT EXISTS BookBuster;
USE BookBuster;

-- First: Create independent tables (no foreign keys)
CREATE TABLE IF NOT EXISTS Provinces(
    ProvinceID INT PRIMARY KEY NOT NULL,
    Province VARCHAR(50) NOT NULL UNIQUE
);

CREATE TABLE IF NOT EXISTS Cities(
    CityID INT PRIMARY KEY,
    City VARCHAR(50) NOT NULL,
    ProvinceID INT NOT NULL,
    CONSTRAINT FOREIGN KEY fk_Province (ProvinceID) REFERENCES Provinces(ProvinceID)
);

CREATE TABLE IF NOT EXISTS Addresses(
    AddressID INT PRIMARY KEY,
    Street VARCHAR(50) NOT NULL,
    CityID INT NOT NULL,
    CONSTRAINT FOREIGN KEY fk_city (CityID) REFERENCES Cities(CityID)
);

-- Now create Books and related tables
CREATE TABLE IF NOT EXISTS Books(
    BookID INT PRIMARY KEY,
    Title VARCHAR(50) NOT NULL,
    Author VARCHAR(30) NOT NULL,
    PublishingYear YEAR NOT NULL,
    BookLanguage VARCHAR(15) NOT NULL,
    RentalStatus ENUM('Rented', 'Available') NOT NULL
);

CREATE TABLE IF NOT EXISTS Genres(
    GenreID INT PRIMARY KEY,
    Genre VARCHAR(20) NOT NULL UNIQUE
);

CREATE TABLE IF NOT EXISTS BookGenre(
    BookID INT NOT NULL,
    GenreID INT NOT NULL,
    PRIMARY KEY(BookID, GenreID),
    FOREIGN KEY (BookID) REFERENCES Books(BookID),
    FOREIGN KEY (GenreID) REFERENCES Genres(GenreID)
);

-- Now create Staff and Roles tables
CREATE TABLE IF NOT EXISTS Staff(
    StaffID INT PRIMARY KEY,
    FirstName VARCHAR(15) NOT NULL,
    LastName VARCHAR(15) NOT NULL,
    Email VARCHAR(50) UNIQUE,
    PhoneNumber VARCHAR(20) UNIQUE
);

CREATE TABLE IF NOT EXISTS Roles(
    RoleID INT PRIMARY KEY,
    RoleName VARCHAR(15) NOT NULL UNIQUE
);

CREATE TABLE IF NOT EXISTS StaffRole(
    StaffID INT NOT NULL,
    RoleID INT NOT NULL,
    PRIMARY KEY(StaffID, RoleID),
    FOREIGN KEY (StaffID) REFERENCES Staff(StaffID),
    FOREIGN KEY (RoleID) REFERENCES Roles(RoleID)
);

-- Now create Members (depends on Addresses)
CREATE TABLE IF NOT EXISTS Members(
    MemberID INT PRIMARY KEY,
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
    LoanID INT PRIMARY KEY,
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
    FineID INT PRIMARY KEY,
    LoanID INT NOT NULL UNIQUE,
    FOREIGN KEY (LoanID) REFERENCES Loans(LoanID)
);

-- Now create Payments (depends on Fines) 
CREATE TABLE IF NOT EXISTS Payments(
    PaymentID INT PRIMARY KEY,
    FineID INT NOT NULL,
    AmountPaid DECIMAL(5,2) NOT NULL,  
    PaymentDate DATE NOT NULL,
    PaymentMethod VARCHAR(20) NOT NULL,
    FOREIGN KEY (FineID) REFERENCES Fines(FineID)
);

-- Insert data into Provinces
INSERT IGNORE INTO Provinces VALUES(1, 'Ontario');

-- Insert data into Cities
INSERT IGNORE INTO Cities VALUES (1, 'Waterloo', 1);

-- Insert data into Addresses
INSERT IGNORE INTO Addresses VALUES
    (1, '123 Elm St', 1),
    (2, '505 Ezra Ave', 1),
    (3, '32 Uptown Blvd', 1);

-- Insert data into Books
INSERT IGNORE INTO Books VALUES
    (2001, 'Dune', 'Frank Herbert', 1965, 'English', 'Available'),
    (2002, 'Harry Potter and the Philosophers Stone', 'J.K. Rowling', 1997, 'English', 'Available'),
    (2003, 'Fight Club', 'Chuck Palahniuk', 1996, 'English', 'Available'),
    (2004, 'Red Fish Blue Fish', 'Dr. Seuss', 1960, 'English', 'Rented');

-- Insert data into Genres
INSERT IGNORE INTO Genres VALUES
    (1, 'Sci-Fi'),
    (2, 'Thriller'),
    (3, 'Poetry'),
    (4, 'Fantasy'),
    (5, 'Childrens'),
    (6, 'Mystery');

-- Insert data into BookGenre
INSERT IGNORE INTO BookGenre VALUES
    (2001, 1),
    (2001, 2),
    (2002, 5),
    (2002, 4),
    (2003, 2),
    (2003, 6),
    (2004, 5),
    (2004, 3);

-- Insert data into Staff
INSERT IGNORE INTO Staff VALUES
    (301, 'Tushar', 'Upadhyay', 'Tupadhyay1@conestogac.on.ca', '519-748-5221'),
    (302, 'Norbert', 'Mika', 'nmika@conestogac.on.ca', '519-748-5222'),
    (303, 'Sean', 'Clarke', 'SfClarke@conestogac.on.ca', '519-748-5223');

-- Insert data into Roles
INSERT IGNORE INTO Roles VALUES
    (1, 'Librarian'),
    (2, 'Manager'),
    (3, 'Housekeeping'),
    (4, 'Reception');

-- Insert data into StaffRole
INSERT IGNORE INTO StaffRole VALUES
    (301, 2),
    (301, 1),
    (302, 1),
    (302, 4),
    (303, 1),
    (303, 4);

-- Insert data into Members
INSERT IGNORE INTO Members VALUES
    (201, 'Zemmat', 'Hagos', '2025-09-01', 'zhagos4339@conestogac.on.ca', '555-878-9946', 1),
    (202, 'Najaf', 'Ali', '2025-09-01', 'nali0484@conestogac.on.ca', '555-878-8887', 2),
    (203, 'Brad', 'Kajganich', '2025-09-01', 'bkajganich6321@conestogac.on.ca', '123-456-7891', 3);

-- Insert data into Loans
INSERT IGNORE INTO Loans VALUES
    (1001, 201, 2001, 302, '2025-11-12', '2025-11-19', '2025-11-21'),
    (1002, 202, 2002, 301, '2025-11-14','2025-11-21', '2025-11-25'),
    (1003, 203, 2003, 303, '2025-11-12', '2025-11-19', '2025-12-21'),
    (1004, 203, 2004, 303, '2025-12-04', '2025-12-11', '2025-12-05');

-- Insert data into Fines
INSERT IGNORE INTO Fines VALUES
    (101, 1001),
    (102, 1002),
    (103, 1003);

-- Insert data into Payments
INSERT IGNORE INTO Payments VALUES
    (1, 101, 2.00, '2025-12-01', 'Mastercard'),
    (2, 102, 2.00, '2025-11-28', 'Visa');

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

-- Test queries to verify views work correctly
SELECT * FROM LoanFineDetails;
SELECT * FROM MemberBalances;