using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using TienAnhGold.Data;
using TienAnhGold.Models;

namespace TienAnhGold.Controllers
{
    [Authorize(Roles = "Admin,Employee")]
    public class GoldsController : Controller
    {
        private readonly TienAnhGoldContext _context;

        public GoldsController(TienAnhGoldContext context)
        {
            _context = context;
        }

        // GET: Golds
        public async Task<IActionResult> Index()
        {
            return View(await _context.Gold.ToListAsync());
        }

        // GET: Golds/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var gold = await _context.Gold
                .FirstOrDefaultAsync(m => m.Id == id);
            if (gold == null)
            {
                return NotFound();
            }

            return View(gold);
        }

        // GET: Golds/Create
        public IActionResult Create()
        {
            return View(new Gold());
        }

        // POST: Golds/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        // POST: Golds/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,Name,ImagePath,Material,Weight,Description,Price,Quantity,IsOutOfStock,ImageFile")] Gold gold)
        {
            if (ModelState.IsValid)
            {
                // Xử lý ảnh nếu có
                if (gold.ImageFile != null && gold.ImageFile.Length > 0)
                {
                    var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images", gold.ImageFile.FileName);
                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await gold.ImageFile.CopyToAsync(stream);
                    }
                    gold.ImagePath = "/images/" + gold.ImageFile.FileName;
                }

                // Cập nhật IsOutOfStock dựa trên Quantity
                gold.IsOutOfStock = gold.Quantity <= 0;

                _context.Add(gold);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(gold);
        }


        // GET: Golds/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var gold = await _context.Gold.FindAsync(id);
            if (gold == null)
            {
                return NotFound();
            }
            return View(gold);
        }

        // POST: Golds/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        // POST: Golds/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Name,ImagePath,Material,Weight,Description,Price,Quantity,IsOutOfStock,ImageFile")] Gold gold)
        {
            if (id != gold.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    var existingGold = await _context.Gold.FindAsync(id);
                    if (existingGold == null)
                    {
                        return NotFound();
                    }

                    // Cập nhật thông tin sản phẩm
                    existingGold.Name = gold.Name;
                    existingGold.Material = gold.Material;
                    existingGold.Weight = gold.Weight;
                    existingGold.Description = gold.Description;
                    existingGold.Price = gold.Price;
                    existingGold.Quantity = gold.Quantity;

                    // Cập nhật IsOutOfStock dựa trên Quantity
                    existingGold.IsOutOfStock = gold.Quantity <= 0;

                    // Xử lý ảnh mới (nếu có)
                    if (gold.ImageFile != null && gold.ImageFile.Length > 0)
                    {
                        var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images", gold.ImageFile.FileName);
                        using (var stream = new FileStream(filePath, FileMode.Create))
                        {
                            await gold.ImageFile.CopyToAsync(stream);
                        }
                        existingGold.ImagePath = "/images/" + gold.ImageFile.FileName;
                    }

                    _context.Update(existingGold);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!GoldExists(gold.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            return View(gold);
        }


        // GET: Golds/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var gold = await _context.Gold
                .FirstOrDefaultAsync(m => m.Id == id);
            if (gold == null)
            {
                return NotFound();
            }

            return View(gold);
        }

        // POST: Golds/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var gold = await _context.Gold.FindAsync(id);
            if (gold != null)
            {
                _context.Gold.Remove(gold);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool GoldExists(int id)
        {
            return _context.Gold.Any(e => e.Id == id);
        }
    }
}
