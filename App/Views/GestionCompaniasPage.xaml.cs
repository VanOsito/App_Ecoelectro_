using System;
using System.Collections.Generic;
using System.Linq;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using App.Data;
using App.Models;
using App.Services;
using Microsoft.Maui.Controls;

namespace App.Views;

public partial class GestionCompaniasPage : ContentPage
{
    // Inyecta tus servicios en el ctor
    private readonly DatabaseService _db;
    private readonly IRegionComunaService _regions; // implementa este servicio para leer tu JSON

    // UI-bindings
    public ObservableCollection<CompanyRow> Companies { get; } = new();
    public ObservableCollection<string> Regiones { get; } = new();
    public ObservableCollection<CoverageItem> Coberturas { get; } = new();
    public ObservableCollection<ComponentOption> Componentes { get; } = new();

    public CompanyAggregate Editing { get; set; } = new(); // modelo en edición
    public bool Busy { get; set; }
    public bool CanDelete => Editing?.PickupCompanyId is not null;

    // respaldo para filtrar
    private List<CompanyRow> _allRows = new();

    public GestionCompaniasPage(DatabaseService db, IRegionComunaService regions)
    {
        InitializeComponent();
        _db = db;
        _regions = regions;

        BindingContext = this;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await LoadInitialAsync();
    }

    private async Task LoadInitialAsync()
    {
        try
        {
            Busy = true; OnPropertyChanged(nameof(Busy));

            // Regiones
            Regiones.Clear();
            var regs = await _regions.GetRegionesAsync();
            foreach (var r in regs) Regiones.Add(r);

            // Componentes
            Componentes.Clear();
            var comps = await _db.GetAllComponentsAsync(); // devuelve Id + Nombre
            foreach (var c in comps) Componentes.Add(c);

            // Compañías
            _allRows = await _db.GetCompaniesBasicAsync();
            Companies.ReplaceWith(_allRows);

            // limpiar formulario
            NewEditing();
        }
        finally
        {
            Busy = false; OnPropertyChanged(nameof(Busy));
        }
    }

    private void NewEditing()
    {
        Editing = new CompanyAggregate();
        Coberturas.Clear();
        foreach (var c in Componentes) c.IsSelected = false;
        OnPropertyChanged(nameof(Editing));
        OnPropertyChanged(nameof(CanDelete));
    }

    // ==== LISTA ====

    private async void OnRefreshClicked(object sender, EventArgs e)
    {
        await LoadInitialAsync();
    }

    // (mantengo SelectionChanged handler por compatibilidad, pero la UI usa tap)
    private async void OnCompanySelected(object sender, SelectionChangedEventArgs e)
    {
        if (e.CurrentSelection?.FirstOrDefault() is not CompanyRow row) return;
        await LoadForEdit(row.PickupCompanyId);
        CompaniesList.SelectedItem = null; // limpiar selección para poder tocar el mismo item otra vez
    }

    // NUEVO: manejador de tap en el ItemTemplate (si el SelectionChanged no se dispara)
    private async void OnCompanyTapped(object sender, EventArgs e)
    {
        // sender será el Frame; su BindingContext es el item
        if (sender is BindableObject bo && bo.BindingContext is CompanyRow row)
        {
            await LoadForEdit(row.PickupCompanyId);
            // limpiar selección por si acaso
            CompaniesList.SelectedItem = null;
        }
    }

    private async Task LoadForEdit(int companyId)
    {
        try
        {
            Busy = true; OnPropertyChanged(nameof(Busy));
            var agg = await _db.GetCompanyAggregateAsync(companyId);
            if (agg is null) return;

            Editing = agg;
            OnPropertyChanged(nameof(Editing));
            OnPropertyChanged(nameof(CanDelete));

            // Coberturas
            Coberturas.Clear();
            foreach (var (reg, com) in agg.Coberturas)
                Coberturas.Add(new CoverageItem { Region = reg, Comuna = com });

            // Componentes (check)
            foreach (var c in Componentes)
                c.IsSelected = agg.ComponentesIds.Contains(c.Id);
        }
        finally
        {
            Busy = false; OnPropertyChanged(nameof(Busy));
        }
    }

    private void OnNewCompany(object sender, EventArgs e) => NewEditing();

    private async void OnDelete(object sender, EventArgs e)
    {
        if (Editing?.PickupCompanyId is null) return;
        var ok = await DisplayAlert("Confirmar", "¿Eliminar esta compañía? Se borrarán coberturas y componentes asociados.", "Eliminar", "Cancelar");
        if (!ok) return;

        var done = await _db.DeleteCompanyAsync(Editing.PickupCompanyId.Value);
        if (done)
        {
            await DisplayAlert("OK", "Compañía eliminada.", "OK");
            await LoadInitialAsync();
        }
        else
        {
            await DisplayAlert("Error", "No se pudo eliminar.", "OK");
        }
    }

    // ==== COBERTURAS ====

    private async void OnRegionChanged(object sender, EventArgs e)
    {
        // cargar comunas de la región seleccionada
        var region = RegionPicker.SelectedItem?.ToString();
        ComunaPicker.ItemsSource = null;
        if (string.IsNullOrWhiteSpace(region)) return;
        var comunas = await _regions.GetComunasAsync(region);
        ComunaPicker.ItemsSource = comunas;
    }

    private void OnAddCoverage(object sender, EventArgs e)
    {
        var region = RegionPicker.SelectedItem?.ToString();
        var comuna = ComunaPicker.SelectedItem?.ToString();

        if (string.IsNullOrWhiteSpace(region))
        {
            DisplayAlert("Aviso", "Selecciona una región.", "OK");
            return;
        }

        // Evitar duplicados (reg, comuna)
        if (Coberturas.Any(c => c.Region == region && c.Comuna == comuna)) return;

        Coberturas.Add(new CoverageItem { Region = region, Comuna = comuna });
    }

    private void OnRemoveCoverage(object sender, EventArgs e)
    {
        if (sender is Button btn && btn.CommandParameter is CoverageItem item)
            Coberturas.Remove(item);
    }

    // ==== COMPONENTES ====


    // ==== GUARDAR ====

    private async void OnSave(object sender, EventArgs e)
    {
        if (string.IsNullOrWhiteSpace(Editing.Nombre))
        {
            await DisplayAlert("Validación", "El nombre es obligatorio.", "OK");
            return;
        }

        // armar aggregate desde UI
        Editing.Coberturas = Coberturas.Select(c => (c.Region, c.Comuna)).ToList();
        Editing.ComponentesIds = Componentes.Where(c => c.IsSelected).Select(c => c.Id).ToList();

        try
        {
            Busy = true; OnPropertyChanged(nameof(Busy));
            var id = await _db.UpsertCompanyAsync(Editing);
            await DisplayAlert("OK", "Compañía guardada.", "OK");

            // refrescar lista y re-cargar edición (por si es creación)
            _allRows = await _db.GetCompaniesBasicAsync();
            Companies.ReplaceWith(_allRows);

            await LoadForEdit(id);
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", ex.Message, "OK");
        }
        finally
        {
            Busy = false; OnPropertyChanged(nameof(Busy));
        }
    }

    // ==== BUSCAR ====

    private void OnSearchTextChanged(object sender, TextChangedEventArgs e)
    {
        var q = (e.NewTextValue ?? "").Trim().ToLowerInvariant();
        if (string.IsNullOrEmpty(q))
        {
            Companies.ReplaceWith(_allRows);
            return;
        }

        var filtered = _allRows.Where(r =>
            (r.Nombre ?? "").ToLowerInvariant().Contains(q)).ToList();

        Companies.ReplaceWith(filtered);
    }
}

// ===== Helpers de UI =====

public class CoverageItem
{
    public string Region { get; set; } = "";
    public string? Comuna { get; set; }
}

// Reemplazo rápido para actualizar ObservableCollection en bloque
public static class ObservableExtensions
{
    public static void ReplaceWith<T>(this ObservableCollection<T> col, IEnumerable<T> items)
    {
        col.Clear();
        foreach (var it in items) col.Add(it);
    }
}



