using G2.Infrastructure.Model;
using G2.Service.PromoCode;
using G2.Service.PromoCode.Dto.Receiving;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace G2.API.Controllers
{
    [Route("api/promo")]
    [ApiController]
    [Authorize]
    public class PromoCodeController : ControllerBase
    {
        private readonly IPromoCodeService _promoCodeService;

        public PromoCodeController(IPromoCodeService promoCodeService)
        {
            _promoCodeService = promoCodeService;
        }

        [HttpGet("{id}")]
        public async Task<Response> GetPromoCode(long id)
        {
            return await _promoCodeService.GetPromoCode(id);
        }

        [HttpGet]
        public async Task<Response> GetAllPromoCodes([FromQuery] int page = 1, [FromQuery] int limit = 10)
        {
            return await _promoCodeService.GetAllPromoCodes(page, limit);
        }

        [HttpPost]
        public async Task<Response> AddPromoCode(AddPromoCodeDto addPromoCodeDto)
        {
            return await _promoCodeService.AddPromoCode(addPromoCodeDto);
        }

        [HttpPost("verify")]
        public async Task<Response> VerifyPromoCode(VerifyPromoCodeDto verifyPromoCodeDto)
        {
            return await _promoCodeService.VerifyPromoCode(verifyPromoCodeDto);
        }

        [HttpDelete("{id}")]
        public async Task<Response> DeletePromoCode(long id)
        {
            return await _promoCodeService.DeletePromoCode(id);
        }

        [HttpPut("{id}")]
        public async Task<Response> UpdatePromoCode(long id, UpdatePromoCodeDto updatePromoCodeDto)
        {
            return await _promoCodeService.UpdatePromoCode(id, updatePromoCodeDto);
        }
    }
}