// PropamaPOS/Controllers/ItemController.cs
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using PropamaPOS.Data;
using PropamaPOS.Models;
using PropamaPOS.Models.ViewModels;

namespace PropamaPOS.Controllers
{
    [Authorize(Roles = "Admin")]
    public class ItemController : Controller
    {
        private readonly AppDbContext _context;
        public ItemController(AppDbContext context) => _context = context;

        // GET: Item
        public async Task<IActionResult> Index()
        {
            var items = await _context.Items
                .Where(i => i.Activo)
                .Include(i => i.Categoria)
                .Include(i => i.Presentaciones.Where(p => p.Activo))
                    .ThenInclude(p => p.UnidadMedida)
                .ToListAsync();
            return View(items);
        }


        // GET: Item/Crear
        public async Task<IActionResult> Crear()
        {
            ViewBag.Unidades = await _context.UnidadesMedida.ToListAsync();
            ViewBag.Categorias = new SelectList(_context.Categorias, "Id_Categoria", "Nombre");
            return View(new ItemViewModel());
        }

        // POST: Item/Crear
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Crear(ItemViewModel model)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.Unidades = await _context.UnidadesMedida.ToListAsync();
                ViewBag.Categorias = new SelectList(_context.Categorias, "Id_Categoria", "Nombre", model.Id_Categoria);
                return View(model);
            }

            // Validar duplicados en el POST
            var duplicateUnit = model.Presentaciones
                .GroupBy(p => p.Id_UnidadMedida)
                .Where(g => g.Count() > 1)
                .Select(g => g.Key)
                .FirstOrDefault();

            if (duplicateUnit > 0)
            {
                ModelState.AddModelError("", "Hay presentaciones repetidas (misma unidad). Elimine duplicados antes de guardar.");
                ViewBag.Unidades = await _context.UnidadesMedida.ToListAsync();
                return View(model);
            }

            var item = new Item
            {
                Nombre = model.Nombre,
                Descripcion = model.Descripcion,
                Codigo = model.Codigo,
                Activo = model.Activo,
                Id_Categoria = model.Id_Categoria
            };

            _context.Items.Add(item);
            await _context.SaveChangesAsync();

            // Guardar presentaciones (no se piden precios aquí)
            if (model.Presentaciones != null && model.Presentaciones.Any())
            {
                foreach (var p in model.Presentaciones)
                {
                    if (p.Id_UnidadMedida <= 0) continue;
                    var present = new ItemPresentacion
                    {
                        Id_Item = item.Id_Item,
                        Id_UnidadMedida = p.Id_UnidadMedida,
                        Cantidad = p.Cantidad > 0 ? p.Cantidad : 1,
                        PrecioVenta = 0m,    // se calculará desde compras
                        PrecioCosto = null,  // nulo hasta primera compra
                        Activo = true

                    }; 
                    _context.ItemPresentaciones.Add(present);
                }
                await _context.SaveChangesAsync();
            }

            TempData["SuccessMessage"] = "Producto creado correctamente.";
            return RedirectToAction(nameof(Index));
        }

        // GET: Item/Editar/5
        public async Task<IActionResult> Editar(int id)
        {
            var item = await _context.Items
                .Include(i => i.Presentaciones)
                .FirstOrDefaultAsync(i => i.Id_Item == id);

            if (item == null) return NotFound();

            var model = new ItemViewModel
            {
                Id_Item = item.Id_Item,
                Nombre = item.Nombre,
                Descripcion = item.Descripcion,
                Codigo = item.Codigo,
                Id_Categoria = item.Id_Categoria,
                Presentaciones = item.Presentaciones
                    .Where(p => p.Activo)
                    .Select(p => new ItemPresentacionViewModel
                    {
                        Id_ItemPresentacion = p.Id_ItemPresentacion,
                        Id_UnidadMedida = p.Id_UnidadMedida,
                        Cantidad = p.Cantidad
                    }).ToList(),
                Activo = item.Activo
            };

            ViewBag.Unidades = await _context.UnidadesMedida.ToListAsync();
            ViewBag.Categorias = new SelectList(_context.Categorias, "Id_Categoria", "Nombre");
            return View(model);
        }

        // POST: Item/Editar/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Editar(int id, ItemViewModel model)
        {
            if (id != model.Id_Item) return NotFound();

            if (!ModelState.IsValid)
            {
                ViewBag.Unidades = await _context.UnidadesMedida.ToListAsync();
                ViewBag.Categorias = new SelectList(_context.Categorias, "Id_Categoria", "Nombre", model.Id_Categoria);
                return View(model);
            }

            // Validar duplicados en el POST
            var duplicateUnit = model.Presentaciones
                .GroupBy(p => p.Id_UnidadMedida)
                .Where(g => g.Count() > 1)
                .Select(g => g.Key)
                .FirstOrDefault();

            if (duplicateUnit > 0)
            {
                ModelState.AddModelError("", "Hay presentaciones repetidas (misma unidad). Elimine duplicados antes de guardar.");
                ViewBag.Unidades = await _context.UnidadesMedida.ToListAsync();
                return View(model);
            }

            var item = await _context.Items
                .Include(i => i.Presentaciones)
                .FirstOrDefaultAsync(i => i.Id_Item == id);

            if (item == null) return NotFound();

            item.Nombre = model.Nombre;
            item.Descripcion = model.Descripcion;
            item.Codigo = model.Codigo;
            item.Activo = model.Activo;
            item.Id_Categoria = model.Id_Categoria;

            // Presentaciones: detectadas por Id_ItemPresentacion si existen
            var postedIds = model.Presentaciones.Where(p => p.Id_ItemPresentacion.HasValue)
                                .Select(p => p.Id_ItemPresentacion!.Value).ToList();

            // Marcar como inactivas las presentaciones que NO están en postedIds
            var toDeactivate = item.Presentaciones.Where(p => !postedIds.Contains(p.Id_ItemPresentacion)).ToList();
            foreach (var p in toDeactivate)
            {
                // soft-delete: inactivar
                p.Activo = false;
                _context.ItemPresentaciones.Update(p);
            }

            // Actualizar existentes y agregar nuevas
            foreach (var p in model.Presentaciones)
            {
                if (p.Id_ItemPresentacion.HasValue)
                {
                    var existing = item.Presentaciones.FirstOrDefault(x => x.Id_ItemPresentacion == p.Id_ItemPresentacion.Value);
                    if (existing != null)
                    {
                        // Si esta presentacion fue previamente inactivada, reactívala
                        existing.Activo = true;
                        existing.Id_UnidadMedida = p.Id_UnidadMedida;
                        existing.Cantidad = p.Cantidad;
                        _context.ItemPresentaciones.Update(existing);
                    }
                }
                else
                {
                    // nuevo
                    var np = new ItemPresentacion
                    {
                        Id_Item = item.Id_Item,
                        Id_UnidadMedida = p.Id_UnidadMedida,
                        Cantidad = p.Cantidad,
                        PrecioVenta = 0m,
                        PrecioCosto = null,
                        Activo = true
                    };
                    _context.ItemPresentaciones.Add(np);
                }
            }

            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Producto actualizado correctamente.";
            return RedirectToAction(nameof(Index));
        }

        // GET: Item/Eliminar/5
        public async Task<IActionResult> Eliminar(int id)
        {
            var item = await _context.Items.FindAsync(id);
            if (item == null) return NotFound();
            return View(item);
        }

        // POST: Item/Eliminar/5  -> Convertir a soft delete
        [HttpPost]
        [ActionName("Eliminar")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EliminarConfirmado(int id)
        {
            var item = await _context.Items
                .Include(i => i.Presentaciones)
                .FirstOrDefaultAsync(i => i.Id_Item == id);
            if (item != null)
            {
                // Soft-delete
                item.Activo = false;
                foreach (var p in item.Presentaciones)
                {
                    p.Activo = false;
                }
                _context.Items.Update(item);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Producto desactivado (soft-delete).";
            }
            return RedirectToAction(nameof(Index));
        }
    }
}
