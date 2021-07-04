namespace DeltaX.Modules.Shift.Repositories
{
	public class ShiftQueries
	{
		public const string sqlCreateTables = @"

CREATE TABLE IF NOT EXISTS `ShiftProfile` (
	`idShiftProfile` 	INT(11) NOT NULL AUTO_INCREMENT,  
	`Name`           	TEXT,
	`CycleDays`			INT(4), 
	`Start`  			DATETIME(3),
	`End`				DATETIME(3), 
	`Enable`			BOOL NOT NULL DEFAULT (1),
	`CreatedAt`			DATETIME(3) NULL DEFAULT CURRENT_TIMESTAMP(),
	PRIMARY KEY (`idShiftProfile`)
);
 
 
CREATE TABLE IF NOT EXISTS `Shift` (
	`idShift` 			INT(11) NOT NULL AUTO_INCREMENT,  
	`idShiftProfile` 	INT(11) REFERENCES `ShiftProfile`(`idShiftProfile`),
	`Name`           	TEXT, 
	`Start`  			TIME(0) NOT NULL,
	`End`				TIME(0) NOT NULL,
	`Enable`			BOOL NOT NULL DEFAULT (1),
	`CreatedAt`			DATETIME(3) NULL DEFAULT CURRENT_TIMESTAMP(),
	PRIMARY KEY (`idShift`)
);

 
CREATE TABLE IF NOT EXISTS `Crew` (
	`idCrew` 			INT(11) NOT NULL AUTO_INCREMENT,  
	`idShiftProfile` 	INT(11) REFERENCES `ShiftProfile`(`idShiftProfile`),
	`Name`           	TEXT,
	`Enable`			BOOL NOT NULL DEFAULT (1),
	`CreatedAt`			DATETIME(3) NULL DEFAULT CURRENT_TIMESTAMP(),
	PRIMARY KEY (`idCrew`)
);
 
CREATE TABLE IF NOT EXISTS `Holiday` (
	`idHoliday` 		INT(11) NOT NULL AUTO_INCREMENT,  
	`idShiftProfile` 	INT(11) REFERENCES `ShiftProfile`(`idShiftProfile`),
	`Name`           	TEXT,	
	`Start`  			DATETIME(3),
	`End`				DATETIME(3),
	`Enable`			BOOL NOT NULL DEFAULT (1),
	`CreatedAt`			DATETIME(3) NULL DEFAULT CURRENT_TIMESTAMP(),
	PRIMARY KEY (`idHoliday`)
);

CREATE TABLE IF NOT EXISTS `ShiftHistory` (
	`idShiftHistory` 	INT(11) NOT NULL AUTO_INCREMENT,  
	`idShiftProfile` 	INT(11) REFERENCES `ShiftProfile`(`idShiftProfile`),
	`idShift` 			INT(11) REFERENCES `Shift`(`idShift`),
	`idCrew` 			INT(11) NULL REFERENCES `Crew`(`idCrew`),
	`Start`  			DATETIME(3),
	`End`				DATETIME(3),
	`CreatedAt`			DATETIME(3) NULL DEFAULT CURRENT_TIMESTAMP(),
	PRIMARY KEY (`idShiftHistory`)
);


CREATE INDEX IF NOT EXISTS idxShiftProfileName ON `ShiftProfile` (`Name` ASC );   
CREATE INDEX IF NOT EXISTS idxShiftHistoryStart ON `ShiftHistory` (`Start` ASC ); 

";
	}
}