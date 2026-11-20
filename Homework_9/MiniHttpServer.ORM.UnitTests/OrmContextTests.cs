using Microsoft.VisualStudio.TestTools.UnitTesting;
using MiniHttpServer.ORM;
namespace MiniHttpServer.ORM.UnitTests;
[TestClass]
public class OrmContextTests
{
    [TestMethod] public void EscapeIdentifier_ValidName_AddsQuotes() => Assert.AreEqual("\"Users\"", SqlCommandFactory.EscapeIdentifier("Users"));
    [DataTestMethod][DataRow("Users; DROP TABLE Users")][DataRow("dbo.Users")][DataRow("1Users")][DataRow("")][DataRow(" ")]
    public void EscapeIdentifier_InvalidName_Throws(string name) => Assert.ThrowsException<ArgumentException>(() => SqlCommandFactory.EscapeIdentifier(name));
    [TestMethod] public void SelectAll_UsesValidatedTableName() { var s=SqlCommandFactory.SelectAll("Users");Assert.AreEqual("SELECT * FROM \"Users\"",s.CommandText);Assert.AreEqual(0,s.Parameters.Count); }
    [TestMethod] public void SelectById_UsesParameter() { var s=SqlCommandFactory.SelectById("Users",7);Assert.AreEqual("SELECT * FROM \"Users\" WHERE \"Id\" = @Id",s.CommandText);Assert.AreEqual(7,s.Parameters["@Id"]); }
    [TestMethod] public void Insert_ExcludesId_AndParameterizesValues() { var s=SqlCommandFactory.Insert("Users",new User{Id=99,Name="O'Neil",Email="o@example.test",Age=22});Assert.AreEqual("INSERT INTO \"Users\" (\"Name\", \"Email\", \"Age\") VALUES (@Name, @Email, @Age)",s.CommandText);Assert.AreEqual("O'Neil",s.Parameters["@Name"]);Assert.IsFalse(s.Parameters.ContainsKey("@Id")); }
    [TestMethod] public void Update_ExcludesId_AndUsesIdWhereParameter() { var s=SqlCommandFactory.Update("Users",4,new User{Id=100,Name="N",Email="n@test",Age=30});Assert.AreEqual("UPDATE \"Users\" SET \"Name\" = @Name, \"Email\" = @Email, \"Age\" = @Age WHERE \"Id\" = @Id",s.CommandText);Assert.AreEqual(4,s.Parameters["@Id"]);Assert.IsFalse(s.CommandText.Contains("SET \"Id\"")); }
    [TestMethod] public void Delete_UsesIdParameter() { var s=SqlCommandFactory.Delete("Products",8);Assert.AreEqual("DELETE FROM \"Products\" WHERE \"Id\" = @Id",s.CommandText);Assert.AreEqual(8,s.Parameters["@Id"]); }
    [TestMethod] public void Mapper_MapsSqlValuesToUser() { var u=EntityMapper.Map<User>(new Dictionary<string,object?>{{"Id",2L},{"Name","Maria"},{"Email","m@test"},{"Age",21L}});Assert.AreEqual(2,u.Id);Assert.AreEqual("Maria",u.Name);Assert.AreEqual(21,u.Age); }
    [TestMethod] public void Mapper_MapsProductAndNulls() { var p=EntityMapper.Map<Product>(new Dictionary<string,object?>{{"Id",5},{"Name","Coffee"},{"Price",12.5m},{"Description",null}});Assert.AreEqual(5,p.Id);Assert.AreEqual(12.5m,p.Price);Assert.IsNull(p.Description); }
    [TestMethod] public void Mapper_IgnoresUnknownColumns() { var u=EntityMapper.Map<User>(new Dictionary<string,object?>{{"Other",123},{"Name","Alex"}});Assert.AreEqual("Alex",u.Name);Assert.AreEqual(0,u.Id); }
    [TestMethod] public void Constructor_EmptyConnectionString_Throws() => Assert.ThrowsException<ArgumentException>(()=>new OrmContext(" "));
    [TestMethod] public void Insert_NullEntity_Throws() => Assert.ThrowsException<ArgumentNullException>(()=>SqlCommandFactory.Insert<User>("Users",null!));
    [TestMethod] public void Update_NullEntity_Throws() => Assert.ThrowsException<ArgumentNullException>(()=>SqlCommandFactory.Update<User>("Users",1,null!));
    [TestMethod] public void DifferentModels_GenerateDifferentColumns() { var u=SqlCommandFactory.Insert("Users",new User{Name="A",Email="a@b",Age=1});var p=SqlCommandFactory.Insert("Products",new Product{Name="P",Price=1.2m});Assert.IsTrue(u.CommandText.Contains("\"Email\""));Assert.IsFalse(p.CommandText.Contains("\"Email\""));Assert.IsTrue(p.CommandText.Contains("\"Price\"")); }
    public sealed class User { public int Id{get;set;} public string Name{get;set;}=""; public string Email{get;set;}=""; public int Age{get;set;} }
    public sealed class Product { public int Id{get;set;} public string Name{get;set;}=""; public decimal Price{get;set;} public string? Description{get;set;} }
}
