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
        // Arrange (������������, ������ �����������)
        var mockRepo = new Mock<ICompanyRepository>();
        mockRepo.Setup(repo => (repo.GetAllCompaniesAsync(false)))
            .Returns(Task.FromResult(GetCompanies()));

        // Act (������ ��������)
        var result = mockRepo.Object.GetAllCompaniesAsync(false)
            .GetAwaiter()
            .GetResult()
            .ToList();

        // Assert (���� �������� ���)
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