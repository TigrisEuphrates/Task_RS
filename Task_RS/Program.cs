
using DocumentFormat.OpenXml.Office2016.Drawing.ChartDrawing;
using Task_RS.Interfaces;
using Task_RS.Services;

namespace Task_RS
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);


            builder.Services.AddControllers();
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();

            builder.Services.AddSingleton<IDataService, SqliteDataService>();
            builder.Services.AddSingleton<IExcelMappingService, ExcelMappingService>();

            builder.Services.AddHostedService<MyBackgroundService>();

            var app = builder.Build();

            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.UseHttpsRedirection();

            app.UseAuthorization();


            app.MapControllers();

            app.Run();
        }
    }
}
