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

        // Generar código único
        private async Task<string> GenerarCodigoUnicoAsync()
        {
            var random = new Random();
            string codigo;
            bool existe;
            do
            {
                int longitud = random.Next(15, 21);
                codigo = string.Concat(Enumerable.Range(0, longitud).Select(_ => random.Next(0, 10).ToString()));
                existe = await _context.Items.AnyAsync(i => i.Codigo == codigo);
            } while (existe);
            return codigo;
        }


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

            // --- Forzar comportamiento consistente cuando IsServicio está marcado/no marcado ---
            if (model.IsServicio)
            {
                // 1) Forzar que exista solo 1 presentacion: Unidad y cantidad = 1
                var unidad = await _context.UnidadesMedida.FirstOrDefaultAsync(u => u.Nombre.ToLower() == "unidad");
                int unidadId = unidad != null ? unidad.Id_UnidadMedida : (await _context.UnidadesMedida.OrderBy(u => u.Id_UnidadMedida).Select(u => u.Id_UnidadMedida).FirstOrDefaultAsync());

                // Crear lista con UNA presentación (manteniendo precio si el usuario ya lo puso en la UI)
                decimal? precioManual = model.Presentaciones?.FirstOrDefault()?.PrecioVenta;
                model.Presentaciones = new List<PropamaPOS.Models.ViewModels.ItemPresentacionViewModel>
                {
                    new PropamaPOS.Models.ViewModels.ItemPresentacionViewModel
                    {
                        Id_UnidadMedida = unidadId,
                        Cantidad = 1,
                        PrecioVenta = precioManual
                    }
                };

                // 2) Forzar categoría 'Servicio' si existe
                var catServ = await _context.Categorias.FirstOrDefaultAsync(c => c.Nombre.ToLower() == "servicio");
                if (catServ != null)
                {
                    model.Id_Categoria = catServ.Id_Categoria;
                }
            }
            else
            {
                // Si no es servicio y la categoría seleccionada es la categoría 'Servicio',
                // cambiar a la primera categoría que no sea 'Servicio' (para evitar dejarla por defecto)
                var catServ = await _context.Categorias.FirstOrDefaultAsync(c => c.Nombre.ToLower() == "servicio");
                if (catServ != null && model.Id_Categoria == catServ.Id_Categoria)
                {
                    var other = await _context.Categorias.Where(c => c.Id_Categoria != catServ.Id_Categoria).OrderBy(c => c.Nombre).FirstOrDefaultAsync();
                    if (other != null) model.Id_Categoria = other.Id_Categoria;
                    else model.Id_Categoria = 0; // si no hay otra, dejar 0 (se validará luego)
                }
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

            string codigoGenerado = await GenerarCodigoUnicoAsync();

            var item = new Item
            {
                Nombre = model.Nombre,
                Descripcion = model.Descripcion,
                Codigo = codigoGenerado,
                Activo = model.Activo,
                Id_Categoria = model.Id_Categoria,
                IsServicio = model.IsServicio,
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

            // --- Forzar comportamiento consistente cuando IsServicio está marcado/no marcado ---
            if (model.IsServicio)
            {
                // Buscar unidad "Unidad" o la primera unidad disponible
                var unidad = await _context.UnidadesMedida.FirstOrDefaultAsync(u => u.Nombre.ToLower() == "unidad");
                int unidadId = unidad != null
                    ? unidad.Id_UnidadMedida
                    : await _context.UnidadesMedida.Select(u => u.Id_UnidadMedida).FirstAsync();

                decimal? precioManual = model.Presentaciones?.FirstOrDefault()?.PrecioVenta ?? 0;

                // Buscar si ya existe una presentación para este item con esa unidad
                var existingPres = await _context.ItemPresentaciones
                    .FirstOrDefaultAsync(p => p.Id_Item == model.Id_Item && p.Id_UnidadMedida == unidadId);

                if (existingPres != null)
                {
                    // ✅ Actualizamos la existente
                    existingPres.Cantidad = 1;
                    existingPres.PrecioVenta = precioManual ?? 0;
                    existingPres.Activo = true;
                    _context.ItemPresentaciones.Update(existingPres);

                    // Reflejar también en el modelo (para que no cree una nueva)
                    model.Presentaciones = new List<PropamaPOS.Models.ViewModels.ItemPresentacionViewModel>
            {
                new PropamaPOS.Models.ViewModels.ItemPresentacionViewModel
                {
                    Id_ItemPresentacion = existingPres.Id_ItemPresentacion,
                    Id_UnidadMedida = existingPres.Id_UnidadMedida,
                    Cantidad = existingPres.Cantidad,
                    PrecioVenta = existingPres.PrecioVenta
                }
            };
                }
                else
                {
                    // ✅ Si no existe (caso raro), la creamos
                    model.Presentaciones = new List<PropamaPOS.Models.ViewModels.ItemPresentacionViewModel>
            {
                new PropamaPOS.Models.ViewModels.ItemPresentacionViewModel
                {
                    Id_UnidadMedida = unidadId,
                    Cantidad = 1,
                    PrecioVenta = precioManual
                }
            };
                }

                // 2) Forzar categoría 'Servicio' si existe
                var catServ = await _context.Categorias.FirstOrDefaultAsync(c => c.Nombre.ToLower() == "servicio");
                if (catServ != null)
                {
                    model.Id_Categoria = catServ.Id_Categoria;
                }
            }
            else
            {
                // Si no es servicio y la categoría seleccionada es la categoría 'Servicio',
                // cambiar a la primera categoría que no sea 'Servicio' (para evitar dejarla por defecto)
                var catServ = await _context.Categorias.FirstOrDefaultAsync(c => c.Nombre.ToLower() == "servicio");
                if (catServ != null && model.Id_Categoria == catServ.Id_Categoria)
                {
                    var other = await _context.Categorias
                        .Where(c => c.Id_Categoria != catServ.Id_Categoria)
                        .OrderBy(c => c.Nombre)
                        .FirstOrDefaultAsync();

                    model.Id_Categoria = other != null ? other.Id_Categoria : 0;
                }
            }

            // --- Cargar item original con presentaciones ---
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

            // --- Actualizar propiedades del Item ---
            item.Nombre = model.Nombre;
            item.Descripcion = model.Descripcion;
            item.Codigo = model.Codigo;
            item.Activo = model.Activo;
            item.Id_Categoria = model.Id_Categoria;
            item.IsServicio = model.IsServicio;

            // Si es servicio, asegurarnos stock y costo 0
            if (item.IsServicio)
            {
                item.Stock = 0;
                item.CostoPromedioUnidad = 0m;
            }

            // --- Actualizar presentaciones ---
            var postedIds = model.Presentaciones
                .Where(p => p.Id_ItemPresentacion.HasValue)
                .Select(p => p.Id_ItemPresentacion!.Value)
                .ToList();

            // Desactivar las que se quitaron
            var toDeactivate = item.Presentaciones
                .Where(p => !postedIds.Contains(p.Id_ItemPresentacion))
                .ToList();

            foreach (var p in toDeactivate)
            {
                p.Activo = false;
                _context.ItemPresentaciones.Update(p);
            }

            // Crear o actualizar las presentaciones enviadas
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
            TempData["SuccessMessage"] = item.IsServicio
                ? "Servicio actualizado correctamente."
                : "Producto actualizado correctamente.";

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
