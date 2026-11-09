using Grpc.Core;
using Reviews.Grpc;

namespace Reviews.WebApi.Grpc;

public class ReviewsGrpcService : ReviewsService.ReviewsServiceBase
{
    private readonly ILogger<ReviewsGrpcService> _logger;

    public ReviewsGrpcService(ILogger<ReviewsGrpcService> logger)
    {
        _logger = logger;
    }

    public override Task<ReviewsListReply> GetForProduct(GetForProductRequest request, ServerCallContext context)
    {
        if (request.ProductId <= 0)
        {
            throw new RpcException(new Status(StatusCode.InvalidArgument, "ProductId must be positive"));
        }

        var response = new ReviewsListReply();
        response.Items.Add(new ReviewReply
        {
            Id = Guid.NewGuid().ToString(),
            ProductId = request.ProductId,
            AuthorName = "Олексій",
            Rating = 5,
            Text = "Чудове тренування та високий рівень інструктора!"
        });

        return Task.FromResult(response);
    }
}