SELECT Id, UserName, Email FROM AspNetUsers WHERE Email = 'martin.arend@hxx4.de'


select id from AspNetRoles where Name = 'Mitglied'


insert into AspNetUserRoles (UserId, RoleId)
VALUES ('3a2f1908-1a33-4cbd-a8a6-b8a2655bec2c','5ba59879-50d5-4996-9db1-77cc9033cd45')