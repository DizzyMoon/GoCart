using payment.PaymentModels;

namespace payment.PaymentServices;

public interface IPaymentService
{
    Task<CreatePaymentResponse> Create(CreatePaymentRequest request);
}