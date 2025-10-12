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

            // Validar duplicados entre presentaciones (misma unidad)
            var duplicateUnit = model.Presentaciones
                .Where(p => p.Id_UnidadMedida > 0)
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
                Id_Categoria = model.Id_Categoria,
                IsServicio = model.IsServicio,
                // stock y costo quedan en 0 por defecto; para servicios no se usan
                Stock = model.IsServicio ? 0 : 0,
                CostoPromedioUnidad = 0m
            };

            _context.Items.Add(item);
            await _context.SaveChangesAsync();

            // Guardar presentaciones. Para servicios, se permite especificar PrecioVenta manual.
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
                        PrecioVenta = p.PrecioVenta ?? 0m,    // para servicios o precio manual
                        PrecioCosto = model.IsServicio ? (decimal?)null : null,  // nulo hasta primera compra
                        Activo = true
                    };
                    _context.ItemPresentaciones.Add(present);
                }
                await _context.SaveChangesAsync();
            }

            TempData["SuccessMessage"] = model.IsServicio ? "Servicio creado correctamente." : "Producto creado correctamente.";
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
                IsServicio = item.IsServicio,
                Presentaciones = item.Presentaciones
                    .Where(p => p.Activo)
                    .Select(p => new ItemPresentacionViewModel
                    {
                        Id_ItemPresentacion = p.Id_ItemPresentacion,
                        Id_UnidadMedida = p.Id_UnidadMedida,
                        Cantidad = p.Cantidad,
                        PrecioVenta = p.PrecioVenta
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

            var item = await _context.Items
                .Include(i => i.Presentaciones)
                .FirstOrDefaultAsync(i => i.Id_Item == id);

            if (item == null) return NotFound();

            // Validar duplicados presentaciones
            var duplicateUnit2 = model.Presentaciones
                .Where(p => p.Id_UnidadMedida > 0)
                .GroupBy(p => p.Id_UnidadMedida)
                .Where(g => g.Count() > 1)
                .Select(g => g.Key)
                .FirstOrDefault();

            if (duplicateUnit2 > 0)
            {
                ModelState.AddModelError("", "Hay presentaciones repetidas (misma unidad). Elimine duplicados antes de guardar.");
                ViewBag.Unidades = await _context.UnidadesMedida.ToListAsync();
                return View(model);
            }

            item.Nombre = model.Nombre;
            item.Descripcion = model.Descripcion;
            item.Codigo = model.Codigo;
            item.Activo = model.Activo;
            item.Id_Categoria = model.Id_Categoria;
            item.IsServicio = model.IsServicio;

            // Si ahora es servicio, asegurarnos stock = 0 y costo = 0
            if (item.IsServicio)
            {
                item.Stock = 0;
                item.CostoPromedioUnidad = 0m;
            }

            // Presentaciones: mismo esquema que ya tienes (inactivar las que se quitan, actualizar existentes, crear nuevas)
            var postedIds = model.Presentaciones.Where(p => p.Id_ItemPresentacion.HasValue)
                                .Select(p => p.Id_ItemPresentacion!.Value).ToList();

            var toDeactivate = item.Presentaciones.Where(p => !postedIds.Contains(p.Id_ItemPresentacion)).ToList();
            foreach (var p in toDeactivate)
            {
                p.Activo = false;
                _context.ItemPresentaciones.Update(p);
            }

            foreach (var p in model.Presentaciones)
            {
                if (p.Id_ItemPresentacion.HasValue)
                {
                    var existing = item.Presentaciones.FirstOrDefault(x => x.Id_ItemPresentacion == p.Id_ItemPresentacion.Value);
                    if (existing != null)
                    {
                        existing.Activo = true;
                        existing.Id_UnidadMedida = p.Id_UnidadMedida;
                        existing.Cantidad = p.Cantidad;
                        existing.PrecioVenta = p.PrecioVenta ?? existing.PrecioVenta;
                        if (item.IsServicio)
                            existing.PrecioCosto = null;
                        _context.ItemPresentaciones.Update(existing);
                    }
                }
                else
                {
                    var np = new ItemPresentacion
                    {
                        Id_Item = item.Id_Item,
                        Id_UnidadMedida = p.Id_UnidadMedida,
                        Cantidad = p.Cantidad,
                        PrecioVenta = p.PrecioVenta ?? 0m,
                        PrecioCosto = item.IsServicio ? (decimal?)null : null,
                        Activo = true
                    };
                    _context.ItemPresentaciones.Add(np);
                }
            }

            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = item.IsServicio ? "Servicio actualizado correctamente." : "Producto actualizado correctamente.";
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
