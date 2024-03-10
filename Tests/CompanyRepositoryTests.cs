using Contracts;
using Entities.Models;
using Microsoft.EntityFrameworkCore.ChangeTracking.Internal;
using Moq;
using System.Net.Sockets;

namespace Tests;

public class CompanyRepositoryTests
{
    [Fact]
    public void GetAllCompaniesAsync_ReturnsListOfCompanies_WithASingleCopmany()
    {
        // Arrange (расположение, псевдо репозиторий)
        var mockRepo = new Mock<ICompanyRepository>();
        mockRepo.Setup(repo => (repo.GetAllCompaniesAsync(false)))
            .Returns(Task.FromResult(GetCompanies()));

        // Act (запуск действия)
        var result = mockRepo.Object.GetAllCompaniesAsync(false)
            .GetAwaiter()
            .GetResult()
            .ToList();

        // Assert (тест утвердим что)
        Assert.IsType<List<Company>>(result);
        Assert.Single(result);
    }

    public IEnumerable<Company> GetCompanies()
    {
        return new List<Company>
        {
            new Company
            {
                Id = Guid.NewGuid(),
                Name = "Test Company",
                Country = "Mongolia",
                Address = "908 Ulanbator avenue"
            }
        };
    }
}