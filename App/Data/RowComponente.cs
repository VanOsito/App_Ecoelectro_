namespace App.Data;
public record RowComponente(
    string nombre_dispositivo,
    int componente_id,
    string nombre_componente,
    string? descripcion_componente,
    bool is_recyclable,
    bool is_reusable,
    bool is_sellable,
    bool is_hazardous,
    string? guidance_short,
    string? guidance_url
);