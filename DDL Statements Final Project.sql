CREATE DATABASE BookBuster;
use BookBuster;

CREATE TABLE Books(
	BookID INT PRIMARY KEY,
    Title VARCHAR(50),
    Author VARCHAR(30),
    PublishingYear YEAR,
    BookLanguage VARCHAR(15),
    RentalStatus ENUM('Rented', 'Available') NOT NULL
    );
INSERT INTO Books VALUES
	(2001, 'Dune', 'Frank Herbert', 1965, 'English', 'Available'),
	(2002, 'Harry Potter and the Philosophers Stone', 'J.K. Rowling', 1997, 'English', 'Available'),
	(2003, 'Fight Club', 'Chuck Palahniuk', 1996, 'English', 'Available'),
	(2004, 'Red Fish Blue Fish', 'Dr. Seuss', 1960, 'English', 'Rented');

CREATE TABLE Genres(
	GenreID INT PRIMARY KEY,
    Genre VARCHAR(20)
    );
INSERT INTO Genres VALUES
	(1, 'Sci-Fi'),
	(2, 'Thriller'),
	(3, 'Poetry'),
	(4, 'Fantasy'),
	(5, 'Childrens'),
	(6, 'Mystery');
CREATE TABLE BookGenre(
	BookID INT NOT NULL REFERENCES Books(BookID),
    GenreID INT NOT NULL REFERENCES Genre(GenreID),
    PRIMARY KEY(BookID, GenreID)
    );
INSERT INTO BookGenre VALUES
	(2001, 1),
	(2001, 2),
	(2002, 5),
	(2002, 4),
	(2003, 2),
	(2003, 6),
	(2004, 5),
	(2004, 3);
CREATE TABLE Members(
	MemberID INT PRIMARY KEY,
    FirstName VARCHAR(15) NOT NULL,
    LastName VARCHAR(15) NOT NULL,
    JoinDate DATE NOT NULL,
    Email VARCHAR(30),
    PhoneNumber VARCHAR(20),
    AddressID INT NOT NULL,
    CONSTRAINT
    FOREIGN KEY fk_address (AddressID)
    REFERENCES Addresses (AddressID)
    );
    
ALTER TABLE Members MODIFY COLUMN Email VARCHAR(50);

INSERT INTO Members VALUES
   (201, 'Zemmat', 'Hagos', '25-9-1', 'zhagos4339@conestogac.on.ca', '555-878-9946', 1),
   (202, 'Najaf', 'Ali', '25-9-1', 'nali0484@conestogac.on.ca', '555-878-8887', 2),
   (203, 'Brad', 'Kajganich', '25-9-1', 'bkajganich6321@conestogac.on.ca', '123-456-7891', 3);

SELECT * FROM Members;
   
CREATE TABLE Addresses(
	AddressID INT PRIMARY KEY,
    Street VARCHAR(50),
    CityID INT NOT NULL,
    CONSTRAINT
    FOREIGN KEY fk_city (CityID)
    REFERENCES Cities(CityID)
	);
    
INSERT INTO Addresses VALUES
	(1, '123 Elm St', 1),
	(2, '505 Ezra Ave', 1),
	(3, '32 Uptown Blvd', 1);
    
CREATE TABLE Cities(
	CityID INT PRIMARY KEY,
    City VARCHAR(50),
    ProvinceID INT NOT NULL,
    CONSTRAINT
    FOREIGN KEY fk_Province (ProvinceID)
    REFERENCES Provinces(ProvinceID)
	);

INSERT INTO Cities VALUES (1, 'Waterloo', 1);

CREATE TABLE Provinces(
	ProvinceID INT PRIMARY KEY NOT NULL,
    Province VARCHAR(50) NOT NULL
	);    
    
INSERT INTO Provinces Value(1, 'Ontario');

CREATE TABLE Staff(
	StaffID INT PRIMARY KEY,
	FirstName VARCHAR(15),
    LastName VARCHAR(15),
    Email VARCHAR(30),
	PhoneNumber VARCHAR(20)
    );

ALTER TABLE Staff MODIFY COLUMN Email VARCHAR(50);

INSERT INTO Staff VALUES
(301, 'Tushar', 'Upadhyay', 'Tupadhyay1@conestogac.on.ca', '519-748-5220'),
(302, 'Norbert', 'Mika', 'nmika@conestogac.on.ca', '519-748-5220'),
(303, 'Sean', 'Clarke', 'SfClarke@conestogac.on.ca', '519-748-5220');

CREATE TABLE Roles(
	RoleID INT PRIMARY KEY,
    RoleName VARCHAR(15) NOT NULL
    );
 
 INSERT INTO Roles VALUES
	(1, 'Librarian'),
	(2, 'Manager'),
	(3, 'Housekeeping'),
    (4, 'Reception');
    
CREATE TABLE StaffRole(
	StaffID INT NOT NULL REFERENCES Staff(StaffID),
    RoleID INT NOT NULL REFERENCES Roles(RoleID),
	PRIMARY KEY(StaffID, RoleID)
);

INSERT INTO StaffRole VALUES
	(301, 2),
	(301, 1),
	(302, 1),
    (302, 4),
	(303, 1),
	(303, 4);

CREATE TABLE Loans(
	LoanID INT PRIMARY KEY,
    MemberID INT NOT NULL,
    BookID INT NOT NULL,
	StaffID INT NOT NULL,
    CheckoutDate DATE NOT NULL,
    DueDate DATE NOT NULL,
    ReturnDate DATE,
    CONSTRAINT
    FOREIGN KEY fk_Book (BookID) REFERENCES Books (BookID),
    FOREIGN KEY fk_Member (MemberID) REFERENCES Members (MemberID),
    FOREIGN KEY fk_Staff (StaffID) REFERENCES Staff (StaffID)
    );
 
INSERT INTO Loans VALUES
	(1001, 201, 2001, 302, '2025-11-12', '2025-11-19', '2025-11-21'),
	(1002, 202, 2002, 301, '2025-11-14','2025-11-21', '2025-11-25'),
	(1003, 203, 2003, 303, '2025-11-12', '2025-11-19', '2025-12-21'),
	(1004, 203, 2004, 303, '2025-12-04', '2025-12-11', '2025-12-05');
 
CREATE TABLE Fines(
	FineID INT PRIMARY KEY,
    LoanID INT NOT NULL,
    FOREIGN KEY (LoanID)
    REFERENCES Loans(LoanID));
    
INSERT INTO Fines VALUES
(101, 1001),
(102, 1002),
(103, 1003);

CREATE TABLE Payments(
	PaymentID INT PRIMARY KEY,
    FineID INT NOT NULL,
    AmountPaid DECIMAL(3, 2),
    PaymentDate DATE,
    PaymentMethod VARCHAR(20),
    FOREIGN KEY (FineID)
    REFERENCES Fines(FineID));
    
INSERT INTO Payments VALUES
(1, 101, 2, '2025-12-01', 'Mastercard'),
(2, 102, 2, '2025-11-28', 'Visa');




CREATE VIEW LoanFineDetails AS
SELECT
    l.LoanID,
    l.BookID,
    l.MemberID,
    l.CheckoutDate,
    l.ReturnDate,
    l.DueDate,

    -- computed days late
    GREATEST(
        DATEDIFF(l.ReturnDate, l.DueDate),
        0
    ) AS DaysLate,

    -- computed fine amount (same as days late)
    GREATEST(
        DATEDIFF(l.ReturnDate, l.DueDate),
        0
    ) AS FineAmount,

    -- fine record (NULL if not late)
    f.FineID,

    -- date assessed = returndate (derived)
    l.ReturnDate AS FineAssessedDate,

    -- total payments (NULL if no payments)
    SUM(p.AmountPaid) AS TotalPaid,

    -- balance = fineAmount - payments (NULL if no payments)
    (
        GREATEST(
            DATEDIFF(l.ReturnDate, l.DueDate),
            0
        )
        - SUM(p.AmountPaid)
    ) AS AmountOwing

FROM Loans l
LEFT JOIN Fines f ON f.LoanID = l.LoanID
LEFT JOIN Payments p ON p.FineID = f.FineID

GROUP BY
    l.LoanID,
    l.BookID,
    l.MemberID,
    l.CheckoutDate,
    l.ReturnDate,
    f.FineID;
    
CREATE VIEW MemberBalances AS
SELECT
    members.MemberID,
    CONCAT(members.FirstName, members.LastName) as MemberName,

    -- Total fines (sum of days late)
    SUM(
        CASE 
            WHEN DATEDIFF(loan.ReturnDate, loan.DueDate) > 0 
            THEN DATEDIFF(loan.ReturnDate, loan.DueDate)
            ELSE 0
        END
    ) AS TotalFineAmount,

    -- Total payments
    IFNULL(SUM(pments.AmountPaid), 0) AS TotalPaid,

    -- Balance owing
    IFNULL(
        SUM(
            CASE 
                WHEN DATEDIFF(loan.ReturnDate, loan.DueDate) > 0
                THEN DATEDIFF(loan.ReturnDate, loan.DueDate)
                ELSE 0
            END
        ) ##Sum of Fines (above) minus Sum of Payments (below)
        - SUM(pments.AmountPaid), 0
    ) AS BalanceOwing

FROM Members members
LEFT JOIN Loans loan ON members.MemberID = loan.MemberID
LEFT JOIN Fines fine ON fine.LoanID = loan.LoanID
LEFT JOIN Payments pments ON fine.FineID = pments.FineID
GROUP BY members.MemberID, MemberName;

Select * From MemberBalances;
    