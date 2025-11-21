using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Collections.Generic;
using System;
using System.Threading.Tasks;
using System.Diagnostics;

namespace App.ViewModels
{
    public class BaseViewModel : INotifyPropertyChanged
    {
        private bool _isBusy;
        private string _title = string.Empty;
        private string _loadingMessage = "Cargando...";

        public bool IsBusy
        {
            get => _isBusy;
            set => SetProperty(ref _isBusy, value);
        }

        public string Title
        {
            get => _title;
            set => SetProperty(ref _title, value ?? string.Empty);
        }

        public string LoadingMessage
        {
            get => _loadingMessage;
            set => SetProperty(ref _loadingMessage, value ?? "Cargando...");
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            try
            {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error en OnPropertyChanged: {ex.Message}");
            }
        }

        protected bool SetProperty<T>(ref T backingStore, T value, [CallerMemberName] string propertyName = "")
        {
            if (string.IsNullOrWhiteSpace(propertyName))
                throw new ArgumentNullException(nameof(propertyName), "El nombre de la propiedad no puede estar vacío");

            if (EqualityComparer<T>.Default.Equals(backingStore, value))
                return false;

            backingStore = value;
            OnPropertyChanged(propertyName);
            return true;
        }

        // MÉTODO CORREGIDO - AGREGAR DONDE SE USA EL NAMESPACE System.Threading.Tasks
        protected async Task ExecuteAsyncOperation(Func<Task> operation, string? loadingMessage = null)
        {
            if (IsBusy)
                return;

            try
            {
                if (!string.IsNullOrWhiteSpace(loadingMessage))
                    LoadingMessage = loadingMessage;

                IsBusy = true;
                await operation().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error en operación async: {ex.Message}");
                throw;
            }
            finally
            {
                IsBusy = false;
                LoadingMessage = "Cargando...";
            }
        }

        // MÉTODO CORREGIDO - TIPO DE RETORNO NULLABLE Y VALOR POR DEFECTO EXPLÍCITO
        protected async Task<T?> ExecuteAsyncOperation<T>(Func<Task<T>> operation, string? loadingMessage = null)
        {
            if (IsBusy)
                return default(T?); // Más explícito que solo 'default'

            try
            {
                if (!string.IsNullOrWhiteSpace(loadingMessage))
                    LoadingMessage = loadingMessage;

                IsBusy = true;
                return await operation().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error en operación async con retorno: {ex.Message}");
                throw;
            }
            finally
            {
                IsBusy = false;
                LoadingMessage = "Cargando...";
            }
        }

        // VERSIÓN ALTERNATIVA QUE LANZA EXCEPCIÓN
        protected async Task<T> ExecuteAsyncOperationStrict<T>(Func<Task<T>> operation, string? loadingMessage = null)
        {
            if (IsBusy)
                throw new InvalidOperationException("Ya hay una operación en curso. Espere a que termine.");

            try
            {
                if (!string.IsNullOrWhiteSpace(loadingMessage))
                    LoadingMessage = loadingMessage;

                IsBusy = true;
                return await operation().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error en operación async estricta: {ex.Message}");
                throw;
            }
            finally
            {
                IsBusy = false;
                LoadingMessage = "Cargando...";
            }
        }

        // MÉTODO PARA OPERACIONES QUE NO DEBEN EJECUTARSE SI YA ESTÁ OCUPADO
        protected bool CanExecuteOperation()
        {
            return !IsBusy;
        }

        // MÉTODO MEJORADO PARA MANEJO SEGURO EN UI
        protected void SafeInvoke(Action action, [CallerMemberName] string? caller = null)
        {
            try
            {
                if (action == null)
                {
                    Debug.WriteLine($"SafeInvoke: Action es null desde {caller}");
                    return;
                }
                action();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error en SafeInvoke desde {caller}: {ex.Message}");
                ManejarError(ex, $"SafeInvoke en {caller}");
            }
        }

        // MÉTODO MEJORADO PARA EJECUCIÓN SEGURA CON RETORNO
        protected T? SafeInvoke<T>(Func<T> func, T? defaultValue = default, [CallerMemberName] string? caller = null)
        {
            try
            {
                if (func == null)
                {
                    Debug.WriteLine($"SafeInvoke<T>: Func es null desde {caller}");
                    return defaultValue;
                }
                return func();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error en SafeInvoke<T> desde {caller}: {ex.Message}");
                ManejarError(ex, $"SafeInvoke<T> en {caller}");
                return defaultValue;
            }
        }

        // MÉTODO ASINCRÓNICO SEGURO
        protected async Task SafeInvokeAsync(Func<Task> asyncAction, [CallerMemberName] string? caller = null)
        {
            try
            {
                if (asyncAction == null)
                {
                    Debug.WriteLine($"SafeInvokeAsync: AsyncAction es null desde {caller}");
                    return;
                }
                await asyncAction().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error en SafeInvokeAsync desde {caller}: {ex.Message}");
                ManejarError(ex, $"SafeInvokeAsync en {caller}");
            }
        }

        // MÉTODO ASINCRÓNICO SEGURO CON RETORNO
        protected async Task<T?> SafeInvokeAsync<T>(Func<Task<T>> asyncFunc, T? defaultValue = default, [CallerMemberName] string? caller = null)
        {
            try
            {
                if (asyncFunc == null)
                {
                    Debug.WriteLine($"SafeInvokeAsync<T>: AsyncFunc es null desde {caller}");
                    return defaultValue;
                }
                return await asyncFunc().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error en SafeInvokeAsync<T> desde {caller}: {ex.Message}");
                ManejarError(ex, $"SafeInvokeAsync<T> en {caller}");
                return defaultValue;
            }
        }

        // MÉTODO PARA VERIFICAR CONEXIÓN (EXTENSIBLE)
        protected virtual bool TieneConexionInternet()
        {
            // Implementación básica - extender en ViewModels específicos
            return true;
        }

        // MÉTODO MEJORADO PARA MANEJAR ERRORES
        protected virtual void ManejarError(Exception ex, string contexto = "")
        {
            Debug.WriteLine($"⛔ Error en {contexto}: {ex.Message}");

            // Aquí podrías agregar lógica para:
            // - Enviar errores a un servicio de telemetría
            // - Mostrar mensajes al usuario
            // - Registrar en un archivo de log
        }

        // MÉTODO PARA LIMPIAR RECURSOS (OVERRIDE EN DERIVADAS)
        public virtual void Dispose()
        {
            // Para ser override por ViewModels que necesiten limpieza
        }

        // MÉTODO PARA NOTIFICAR CAMBIOS EN MÚLTIPLES PROPIEDADES
        protected void NotificarCambio([CallerMemberName] string? propiedad = null, params string[] propiedadesAdicionales)
        {
            if (!string.IsNullOrEmpty(propiedad))
                OnPropertyChanged(propiedad);

            foreach (var propAdicional in propiedadesAdicionales)
            {
                if (!string.IsNullOrEmpty(propAdicional))
                    OnPropertyChanged(propAdicional);
            }
        }
    }
}