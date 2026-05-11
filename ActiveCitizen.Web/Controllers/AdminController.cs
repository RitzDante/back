using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ActiveCitizen.Web.Services;
using ActiveCitizen.Web.Models;



namespace ActiveCitizen.Web.Controllers
{
    [Authorize(Roles ="Admin")]
    public class AdminController : Controller
    {
        private readonly IApiClient _apiClient;

        public AdminController(IApiClient apiClient)
        {
            _apiClient = apiClient;
        }

        public async Task<IActionResult> Index()
        {
            var inspectors = await _apiClient.GetAsync<List<InspectorViewModel>>("api/admin/inspectors");
            return View(inspectors ?? new List<InspectorViewModel>());

        }

        public async Task<IActionResult> CreateInspector()
        {
            var districts = await _apiClient.GetAsync<List<DistrictViewModel>>("api/districts");
            ViewBag.Districts = districts ?? new List<DistrictViewModel>();
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateInspector(CreateInspectorViewModel model)
        {
            if (!ModelState.IsValid)
            {
                var districts = await _apiClient.GetAsync<List<DistrictViewModel>>("api/districts");
                ViewBag.Districts = districts ?? new List<DistrictViewModel>();
                return View(model);
            }
            try
            {
                var response = await _apiClient.PostAsync<dynamic>("api/auth/register-inspector", model);
                TempData["SuccessMessage"] = "Инспектор успешно добавлен!";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception)
            {
                ModelState.AddModelError(string.Empty, "Ошибка при добавлении инспектора. Попрбуйте снова.");
                var districts = await _apiClient.GetAsync<List<DistrictViewModel>>("api/districts");
                ViewBag.Districts = districts ?? new List<DistrictViewModel>();
                return View(model);
            }
        }

        public async Task<IActionResult> EditInspector(int id)
        {
            var inspector = await _apiClient.GetAsync<InspectorEditViewModel>($"api/admin/inspector/{id}");
            if (inspector == null)
                return NotFound();

            var districts = await _apiClient.GetAsync<List<DistrictViewModel>>("api/districts");
            ViewBag.Districts = districts ?? new List<DistrictViewModel>();
            return View(inspector);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditInspector(InspectorEditViewModel model)
        {
            if (!ModelState.IsValid)
            {
                var districts = await _apiClient.GetAsync<List<DistrictViewModel>>("api/districts");
                ViewBag.Districts = districts ?? new List<DistrictViewModel>();
                return View(model);
            }

            if (!string.IsNullOrEmpty(model.NewPassword) && model.NewPassword != model.ConfirmPassword)
            {
                ModelState.AddModelError("ConfirmPassword", "Пароли не совпадают");
                var districts = await _apiClient.GetAsync<List<DistrictViewModel>>("api/districts");
                ViewBag.Districts = districts ?? new List<DistrictViewModel>();
                return View(model);
            }

            var updateData = new
            {
                districtId = model.DistrictId,
                password = model.NewPassword
            };

            try
            {
                await _apiClient.PutAsync<object>($"api/admin/inspector/{model.Id}", updateData);
                TempData["SuccessMessage"] = "Данные инспектора обновлены";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception)
            {
                ModelState.AddModelError(string.Empty, "Ошибка при обновлении");
                var districts = await _apiClient.GetAsync<List<DistrictViewModel>>("api/districts");
                ViewBag.Districts = districts ?? new List<DistrictViewModel>();
                return View(model);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteInspector(int id)
        {
            try
            {
                await _apiClient.DeleteAsync($"api/admin/inspector/{id}");
                TempData["SuccessMessage"] = "Инспектор удален";
            }
            catch
            {
                TempData["ErrorMessage"] = "Ошибка при удалени";
            }
            return RedirectToAction(nameof(Index));
        }
    }
}
