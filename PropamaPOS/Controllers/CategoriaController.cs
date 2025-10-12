using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PropamaPOS.Data;
using PropamaPOS.Models;

namespace PropamaPOS.Controllers
{
    [Authorize(Roles = "Admin")]
    public class CategoriaController : Controller
    {
        private readonly AppDbContext _context;

        public CategoriaController(AppDbContext context)
        {
            _context = context;
        }

        // GET: Categoria
        public async Task<IActionResult> Index()
        {
            var categorias = await _context.Categorias.ToListAsync();
            return View(categorias);
        }

        // GET: Categoria/Crear
        public IActionResult Crear()
        {
            return View();
        }

        // POST: Categoria/Crear
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Crear(Categoria model)
        {
            if (ModelState.IsValid)
            {
                _context.Add(model);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Categoría creada exitosamente.";
                return RedirectToAction(nameof(Index));
            }
            return View(model);
        }

        // GET: Categoria/Editar/5
        public async Task<IActionResult> Editar(int id)
        {
            var categoria = await _context.Categorias.FindAsync(id);
            if (categoria == null) return NotFound();
            return View(categoria);
        }

        // POST: Categoria/Editar/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Editar(int id, Categoria model)
        {
            if (id != model.Id_Categoria) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(model);
                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = "Categoría actualizada correctamente.";
                    return RedirectToAction(nameof(Index));
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!_context.Categorias.Any(e => e.Id_Categoria == model.Id_Categoria))
                        return NotFound();
                    else
                        throw;
                }
            }
            return View(model);
        }

        // GET: Categoria/Eliminar/5
        public async Task<IActionResult> Eliminar(int id)
        {
            var categoria = await _context.Categorias.FindAsync(id);
            if (categoria == null) return NotFound();
            return View(categoria);
        }

        // POST: Categoria/EliminarConfirmado/5
        [HttpPost]
        [ActionName("Eliminar")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EliminarConfirmado(int id)
        {
            var categoria = await _context.Categorias
                .Include(c => c.Items)
                .FirstOrDefaultAsync(c => c.Id_Categoria == id);

            if (categoria == null)
                return NotFound();

            // Verificar si tiene productos asociados
            bool tieneProductos = await _context.Items.AnyAsync(i => i.Id_Categoria == id && i.Activo);
            if (tieneProductos)
            {
                TempData["ErrorMessage"] = "No se puede eliminar esta categoría porque tiene productos asociados.";
                return RedirectToAction(nameof(Index));
            }

            _context.Categorias.Remove(categoria);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Categoría eliminada correctamente.";
            return RedirectToAction(nameof(Index));
        }
    }
}
