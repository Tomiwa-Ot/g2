using G2.Infrastructure.Model;
using G2.Service.PromoCode.Dto.Receiving;

namespace G2.Service.PromoCode
{
    public interface IPromoCodeService
    {
        Task<Response> AddPromoCode(AddPromoCodeDto addPromoCodeDto);
        Task<Response> VerifyPromoCode(VerifyPromoCodeDto verifyPromoCodeDto);
        Task<Response> GetAllPromoCodes(int page = 1, int limit = 10);
        Task<Response> GetPromoCode(long id);
        Task<Response> DeletePromoCode(long id);
        Task<Response> UpdatePromoCode(long id, UpdatePromoCodeDto updatePromoCodeDto);
    }
}