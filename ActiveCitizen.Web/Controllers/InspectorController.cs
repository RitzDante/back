using ActiveCitizen.Web.Models;
using ActiveCitizen.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ActiveCitizen.Web.Controllers
{
    [Authorize(Roles = "Inspector")]
    public class InspectorController : Controller
    {
        private readonly IApiClient _apiclient;

        public InspectorController(IApiClient apiclient)
        {
            _apiclient = apiclient;
        }

        public async Task<IActionResult> Index()
        {
            var claims = await _apiclient.GetAsync<List<ClaimViewModel>>("api/claims");
            return View(claims);
        }

        public async Task<IActionResult> Details(int id)
        {
            var claim = await _apiclient.GetAsync<ClaimViewModel>($"api/claims/{id}");
            if (claim == null)
                return NotFound();
            return View(claim);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangeStatus(int id, int newStatusId)
        {
            try
            {
                var response = await _apiclient.PutAsync<object>($"api/claims/{id}/status", new { statusId = newStatusId });
                TempData["SuccessMessage"] = "Статус заявки успешно изменен.";
            }
            catch
            {
                TempData["ErrorMessage"] = "Ошибка при изменении статуса.";
            }
            return RedirectToAction(nameof(Details), new { id });
        }

    }
}
