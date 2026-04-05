using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WMSP.Api.Models.Entities;

[Table("mat_material")]
public class MatMaterial
{
    [Key]
    [Column("material_id")]
    public long MaterialId { get; set; }

    [Column("material_code")]
    [MaxLength(50)]
    public string MaterialCode { get; set; } = null!;

    [Column("material_name")]
    [MaxLength(200)]
    public string MaterialName { get; set; } = null!;

    [Column("barcode")]
    [MaxLength(50)]
    public string? Barcode { get; set; }

    [Column("unit")]
    [MaxLength(20)]
    public string Unit { get; set; } = null!;

    [Column("unit_cost")]
    public decimal? UnitCost { get; set; }
}
