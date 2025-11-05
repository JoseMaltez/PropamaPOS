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
    [Authorize(Roles = "Admin,Empleado")]
    public class ItemController : Controller
    {
        private readonly AppDbContext _context;
        private readonly IConfiguration _config;
        public ItemController(AppDbContext context, IConfiguration config) 
        { 
            _context = context;
            _config = config;
        }

        // Generar código único
        [Authorize(Roles = "Admin")]
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
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Index(string q, int? categoriaId, bool? bajoStock, bool? esServicio, int page = 1)
        {
            const int PageSize = 15;
            decimal IVA = _config.GetValue<decimal?>("Tax:IVA") ?? 0.12m;

            var query = _context.Items
                .Where(i => i.Activo)
                .Include(i => i.Categoria)
                .Include(i => i.Presentaciones.Where(p => p.Activo))
                    .ThenInclude(p => p.UnidadMedida)
                .AsQueryable();

            // Filtro por texto (nombre o código)
            if (!string.IsNullOrWhiteSpace(q))
            {
                var search = q.Trim().ToLower();
                query = query.Where(i =>
                    i.Nombre.ToLower().Contains(search) ||
                    (i.Codigo != null && i.Codigo.ToLower().Contains(search))
                );
            }

            // Filtro por categoría
            if (categoriaId.HasValue && categoriaId.Value > 0)
                query = query.Where(i => i.Id_Categoria == categoriaId.Value);

            // Filtro por bajo stock
            if (bajoStock.HasValue && bajoStock.Value)
                query = query.Where(i => !i.IsServicio && i.Stock <= (i.StockMinimo ?? 0));

            // Filtro por tipo (producto o servicio)
            if (esServicio.HasValue)
                query = query.Where(i => i.IsServicio == esServicio.Value);

            // Paginación
            var total = await query.CountAsync();
            var totalPages = (int)Math.Ceiling(total / (double)PageSize);
            if (page < 1) page = 1;
            if (page > totalPages && totalPages > 0) page = totalPages;

            var items = await query
                .OrderBy(i => i.Nombre)
                .Skip((page - 1) * PageSize)
                .Take(PageSize)
                .ToListAsync();

            var categorias = await _context.Categorias
                .OrderBy(c => c.Nombre)
                .ToListAsync();

            ViewBag.Categorias = categorias;
            ViewBag.CurrentCategoria = categoriaId;
            ViewBag.CurrentQuery = q;
            ViewBag.CurrentBajoStock = bajoStock;
            ViewBag.CurrentEsServicio = esServicio;
            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = totalPages;
            ViewBag.TotalItems = total;
            ViewBag.PageSize = PageSize;
            ViewBag.IVA = IVA;

            return View(items);
        }



        // GET: Item/Crear
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Crear()
        {
            ViewBag.Unidades = await _context.UnidadesMedida.ToListAsync();
            ViewBag.Categorias = new SelectList(_context.Categorias, "Id_Categoria", "Nombre");
            return View(new ItemViewModel());
        }

        // POST: Item/Crear
        [Authorize(Roles = "Admin")]
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

            if (model.IsServicio)
            {
                var unidad = await _context.UnidadesMedida.FirstOrDefaultAsync(u => u.Nombre.ToLower() == "unidad");
                int unidadId = unidad != null ? unidad.Id_UnidadMedida : (await _context.UnidadesMedida.OrderBy(u => u.Id_UnidadMedida).Select(u => u.Id_UnidadMedida).FirstOrDefaultAsync());

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

                var catServ = await _context.Categorias.FirstOrDefaultAsync(c => c.Nombre.ToLower() == "servicio");
                if (catServ != null)
                {
                    model.Id_Categoria = catServ.Id_Categoria;
                }
            }
            else
            {
                var catServ = await _context.Categorias.FirstOrDefaultAsync(c => c.Nombre.ToLower() == "servicio");
                if (catServ != null && model.Id_Categoria == catServ.Id_Categoria)
                {
                    var other = await _context.Categorias.Where(c => c.Id_Categoria != catServ.Id_Categoria).OrderBy(c => c.Nombre).FirstOrDefaultAsync();
                    if (other != null) model.Id_Categoria = other.Id_Categoria;
                    else model.Id_Categoria = 0;
                }
            }
            
            //presentaciones duplicadas
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
                StockMinimo = model.IsServicio ? null : (model.StockMinimo ?? 0),
                CostoPromedioUnidad = 0m
            };



            _context.Items.Add(item);
            await _context.SaveChangesAsync();

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
                        PrecioVenta = p.PrecioVenta ?? 0m,    // para servicios precio manual
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
        [Authorize(Roles = "Admin")]
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
                StockMinimo = item.StockMinimo,
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
        [Authorize(Roles = "Admin")]
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

            if (model.IsServicio)
            {
                var unidad = await _context.UnidadesMedida.FirstOrDefaultAsync(u => u.Nombre.ToLower() == "unidad");
                int unidadId = unidad != null
                    ? unidad.Id_UnidadMedida
                    : await _context.UnidadesMedida.Select(u => u.Id_UnidadMedida).FirstAsync();

                decimal? precioManual = model.Presentaciones?.FirstOrDefault()?.PrecioVenta ?? 0;

                var existingPres = await _context.ItemPresentaciones
                    .FirstOrDefaultAsync(p => p.Id_Item == model.Id_Item && p.Id_UnidadMedida == unidadId);

                if (existingPres != null)
                {
                    existingPres.Cantidad = 1;
                    existingPres.PrecioVenta = precioManual ?? 0;
                    existingPres.Activo = true;
                    _context.ItemPresentaciones.Update(existingPres);

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

                var catServ = await _context.Categorias.FirstOrDefaultAsync(c => c.Nombre.ToLower() == "servicio");
                if (catServ != null)
                {
                    model.Id_Categoria = catServ.Id_Categoria;
                }
            }
            else
            {
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

            var item = await _context.Items
                .Include(i => i.Presentaciones)
                .FirstOrDefaultAsync(i => i.Id_Item == id);

            if (item == null) return NotFound();

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

            item.StockMinimo = item.IsServicio ? null : (model.StockMinimo ?? item.StockMinimo ?? 0);

            if (item.IsServicio)
            {
                item.Stock = 0;
                item.CostoPromedioUnidad = 0m;
            }

            var postedIds = model.Presentaciones
                .Where(p => p.Id_ItemPresentacion.HasValue)
                .Select(p => p.Id_ItemPresentacion!.Value)
                .ToList();

            var toDeactivate = item.Presentaciones
                .Where(p => !postedIds.Contains(p.Id_ItemPresentacion))
                .ToList();

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
            TempData["SuccessMessage"] = item.IsServicio
                ? "Servicio actualizado correctamente."
                : "Producto actualizado correctamente.";

            return RedirectToAction(nameof(Index));
        }

        // GET: Item/Eliminar/5
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Eliminar(int id)
        {
            var item = await _context.Items.FindAsync(id);
            if (item == null) return NotFound();
            return View(item);
        }

        // POST: Item/Eliminar/5 
        [Authorize(Roles = "Admin")]
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

                TempData["SuccessMessage"] = "Producto eliminado.";
            }
            return RedirectToAction(nameof(Index));
        }

        // GET: Item/VerInventario
        public async Task<IActionResult> VerInventario(string q, int? categoriaId, bool? bajoStock, int page = 1)
        {
            const int PageSize = 30;
            decimal IVA = _config.GetValue<decimal?>("Tax:IVA") ?? 0.12m;

            var query = _context.Items
                .Include(i => i.Categoria)
                .Include(i => i.Presentaciones.Where(p => p.Activo))
                    .ThenInclude(p => p.UnidadMedida)
                .Where(i => i.Activo)
                .AsQueryable();

            //Filtro por búsqueda general
            if (!string.IsNullOrWhiteSpace(q))
            {
                q = q.Trim().ToLower();
                query = query.Where(i =>
                    i.Nombre.ToLower().Contains(q) ||
                    (i.Codigo != null && i.Codigo.ToLower().Contains(q))
                );
            }

            //Filtro por categoría
            if (categoriaId.HasValue && categoriaId.Value > 0)
            {
                query = query.Where(i => i.Id_Categoria == categoriaId.Value);
            }

            //Filtro por bajo stock
            if (bajoStock.HasValue && bajoStock.Value)
            {
                query = query.Where(i => !i.IsServicio && i.Stock <= (i.StockMinimo ?? 0));
            }

            //Orden por nombre
            query = query.OrderBy(i => i.Nombre);

            //Paginación
            var total = await query.CountAsync();
            var totalPages = (int)Math.Ceiling(total / (double)PageSize);
            if (page < 1) page = 1;
            if (page > totalPages && totalPages > 0) page = totalPages;

            var items = await query
                .Skip((page - 1) * PageSize)
                .Take(PageSize)
                .ToListAsync();

            //Obtener lista de categorías para el filtro 
            var categorias = await _context.Categorias
                .OrderBy(c => c.Nombre)
                .ToListAsync();

            //Pasar datos a la vista
            ViewBag.Categorias = categorias;
            ViewBag.CurrentCategoria = categoriaId;
            ViewBag.CurrentQuery = q;
            ViewBag.CurrentBajoStock = bajoStock;
            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = totalPages;
            ViewBag.TotalItems = total;
            ViewBag.PageSize = PageSize;
            ViewBag.IVA = IVA;

            return View("VerInventario", items);
        }



    }
}
