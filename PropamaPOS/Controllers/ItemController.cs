using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
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
                .Include(i => i.Proveedor)
                .Include(i => i.Presentaciones)
                    .ThenInclude(p => p.UnidadMedida)
                .ToListAsync();
            return View(items);
        }

        // GET: Item/Crear
        public async Task<IActionResult> Crear()
        {
            ViewBag.Unidades = await _context.UnidadesMedida.ToListAsync();
            ViewBag.Proveedores = await _context.Proveedores.ToListAsync();
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
                ViewBag.Proveedores = await _context.Proveedores.ToListAsync();
                return View(model);
            }

            var item = new Item
            {
                Nombre = model.Nombre,
                Descripcion = model.Descripcion,
                Codigo = model.Codigo,
                Id_Proveedor = model.Id_Proveedor,
                Activo = model.Activo
            };

            _context.Items.Add(item);
            await _context.SaveChangesAsync();

            // Guardar presentaciones
            if (model.Presentaciones != null && model.Presentaciones.Any())
            {
                foreach (var p in model.Presentaciones)
                {
                    // Evitamos presentaciones vacías
                    if (p.Id_UnidadMedida <= 0) continue;
                    var present = new ItemPresentacion
                    {
                        Id_Item = item.Id_Item,
                        Id_UnidadMedida = p.Id_UnidadMedida,
                        Cantidad = p.Cantidad > 0 ? p.Cantidad : 1,
                        PrecioVenta = p.PrecioVenta,
                        PrecioCosto = p.PrecioCosto
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
                Id_Proveedor = item.Id_Proveedor,
                Activo = item.Activo,
                Presentaciones = item.Presentaciones.Select(p => new ItemPresentacionViewModel
                {
                    Id_ItemPresentacion = p.Id_ItemPresentacion,
                    Id_UnidadMedida = p.Id_UnidadMedida,
                    Cantidad = p.Cantidad,
                    PrecioVenta = p.PrecioVenta,
                    PrecioCosto = p.PrecioCosto
                }).ToList()
            };

            ViewBag.Unidades = await _context.UnidadesMedida.ToListAsync();
            ViewBag.Proveedores = await _context.Proveedores.ToListAsync();
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
                ViewBag.Proveedores = await _context.Proveedores.ToListAsync();
                return View(model);
            }

            var item = await _context.Items
                .Include(i => i.Presentaciones)
                .FirstOrDefaultAsync(i => i.Id_Item == id);

            if (item == null) return NotFound();

            item.Nombre = model.Nombre;
            item.Descripcion = model.Descripcion;
            item.Codigo = model.Codigo;
            item.Id_Proveedor = model.Id_Proveedor;
            item.Activo = model.Activo;

            // Actualizar DB
            //  - eliminar presentaciones removidas
            var postedIds = model.Presentaciones.Where(p => p.Id_ItemPresentacion.HasValue).Select(p => p.Id_ItemPresentacion!.Value).ToList();
            var toRemove = item.Presentaciones.Where(p => !postedIds.Contains(p.Id_ItemPresentacion)).ToList();
            _context.ItemPresentaciones.RemoveRange(toRemove);

            //  - actualizar existentes y agregar nuevas
            foreach (var p in model.Presentaciones)
            {
                if (p.Id_ItemPresentacion.HasValue)
                {
                    var existing = item.Presentaciones.FirstOrDefault(x => x.Id_ItemPresentacion == p.Id_ItemPresentacion.Value);
                    if (existing != null)
                    {
                        existing.Id_UnidadMedida = p.Id_UnidadMedida;
                        existing.Cantidad = p.Cantidad;
                        existing.PrecioVenta = p.PrecioVenta;
                        existing.PrecioCosto = p.PrecioCosto;
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
                        PrecioVenta = p.PrecioVenta,
                        PrecioCosto = p.PrecioCosto
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

        // POST: Item/Eliminar/5
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
                _context.ItemPresentaciones.RemoveRange(item.Presentaciones);
                _context.Items.Remove(item);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Producto eliminado.";
            }
            return RedirectToAction(nameof(Index));
        }
    }
}
