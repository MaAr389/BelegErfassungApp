-- Initial Database Setup
USE [master]
GO

IF DB_ID('BelegverwaltungDb') IS NOT NULL
  DROP DATABASE [BelegverwaltungDb]
GO

CREATE DATABASE [BelegverwaltungDb]
GO

USE [BelegverwaltungDb]
GO

-- EF Core wird die Migrations automatisch ausführen
