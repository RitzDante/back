using ActiveCitizen.Web.Models;
using ActiveCitizen.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ActiveCitizen.Web.Controllers
{
    [Authorize(Roles = "Inspector")]
    public class InspectorController : Controller
    {
        private readonly IApiClient _apiClient;

        public InspectorController(IApiClient apiClient)
        {
            _apiClient = apiClient;
        }

        public async Task<IActionResult> Index()
        {
            var claims = await _apiClient.GetAsync<List<ClaimViewModel>>("api/claims");
            return View(claims);
        }

        public async Task<IActionResult> Details(int id)
        {
            var claim = await _apiClient.GetAsync<ClaimViewModel>($"api/claims/{id}");

            if (claim == null)
                return NotFound();

            var statuses = await _apiClient.GetAsync<List<StatusViewModel>>("api/claims/statuses");
            ViewBag.Statuses = statuses;

            return View(claim);
        }

        public async Task<IActionResult> Photo(int id)
        {
            try
            {
                var file = await _apiClient.GetFileAsync($"api/claims/{id}/photo");
                return File(file.Bytes, file.ContentType, file.FileName);
            }
            catch
            {
                return NotFound();
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangeStatus(int id, int newStatusId)
        {
            try
            {
                await _apiClient.PutAsync<object>(
                    $"api/claims/{id}/status",
                    new { statusId = newStatusId }
                );

                TempData["SuccessMessage"] = "Статус заявки успешно изменён.";
            }
            catch
            {
                TempData["ErrorMessage"] = "Ошибка при изменении статуса.";
            }

            return RedirectToAction(nameof(Details), new { id });
        }
    }
}