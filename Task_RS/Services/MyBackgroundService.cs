using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Task_RS.DTOs;
using Task_RS.Interfaces;

public class MyBackgroundService : BackgroundService
{
    private readonly ILogger<MyBackgroundService> _logger;
    private readonly IDataService _dataService;

    public MyBackgroundService(ILogger<MyBackgroundService> logger, IDataService dataService)
    {
        _logger = logger;
        _dataService = dataService;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Background service started.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                _logger.LogInformation("Task executed at: {time}", DateTimeOffset.Now);

                await DoWorkAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while executing background task.");
            }

            await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
        }

        _logger.LogInformation("Background service stopped.");
    }

    private async Task DoWorkAsync(CancellationToken stoppingToken)
    {
        List<Product> pr = await _dataService.GetProductsSortedByPrice();
        List<List<Product>> newPr = new List<List<Product>>();
        decimal price = 0;
        decimal sum = await _dataService.GetSum();
        int iter = 0;
        Product product;

        List<decimal> allPrices = new List<decimal>();

        for (;0 < sum;)
        {
            price = 0;
            newPr.Add(new List<Product>());
            
            foreach (var item in pr)
            {
                for (int i = 1; ; i++)
                {
                    if (item.Quantity==0)
                    {
                        product = new Product();

                        product.Id = item.Id;
                        product.Name = item.Name;
                        product.Unit = item.Unit;
                        product.PriceEur = item.PriceEur;
                        product.Quantity = 0;
                        newPr[iter].Add(product);

                        break;
                    }

                    price += item.PriceEur;

                    if (i == item.Quantity)
                    {
                        if (price > 200)
                        {
                            price -= item.PriceEur;
                            i--;

                            product = new Product();

                            product.Id = item.Id;
                            product.Name = item.Name;
                            product.Unit = item.Unit;
                            product.PriceEur = item.PriceEur;
                            product.Quantity = i;

                            item.Quantity -= i;

                            newPr[iter].Add(product);

                            break;
                        }
                        product = new Product();

                        product.Id = item.Id;
                        product.Name = item.Name;
                        product.Unit = item.Unit;
                        product.PriceEur = item.PriceEur;
                        product.Quantity = i;

                        item.Quantity -= i;

                        newPr[iter].Add(product);

                        break;
                    }


                    if (price > 200)
                    {
                        price -= item.PriceEur;
                        i--;

                        product = new Product();

                        product.Id = item.Id;
                        product.Name = item.Name;
                        product.Unit = item.Unit;
                        product.PriceEur = item.PriceEur;
                        product.Quantity = i;

                        item.Quantity -= i;

                        newPr[iter].Add(product);

                        break;
                    }
                }





            }
            allPrices.Add(price);

            sum -= price;
            iter++;
        }


        foreach (var package in newPr)
        {
            package.RemoveAll(item => item.Quantity == 0);
        }


        //set status as processed
        List<int> ids = new List<int>();
        foreach (var item in pr)
        {
            ids.Add(item.Id);
        }

        await _dataService.SetStatusAsync(ids);

        await _dataService.AddGroupAsync(newPr, allPrices);

    }
}
