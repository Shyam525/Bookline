using Bookline.Infrastructure.Jobs;
using Bookline.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddDbContext<BooklineDbContext>(options =>
{
    options.UseInMemoryDatabase("BooklineDb");
});

builder.Services.AddHostedService<OutboxAndReminderWorker>();

var host = builder.Build();
host.Run();
