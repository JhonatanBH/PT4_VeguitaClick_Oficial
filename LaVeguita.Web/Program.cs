namespace LaVeguita.Web
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // 1. Agregar servicios al contenedor.
            builder.Services.AddControllersWithViews();

            // 2. CONFIGURACIÓN DE SESIONES (Optimizado a 2 min por seguridad)
            builder.Services.AddSession(options =>
            {
                options.IdleTimeout = TimeSpan.FromMinutes(2); // 🚀 Modificado para la defensa
                options.Cookie.HttpOnly = true;
                options.Cookie.IsEssential = true;
            });

            // 3. Mantener el soporte para acceder a la sesión desde cualquier lugar
            builder.Services.AddHttpContextAccessor();

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Home/Error");
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles();

            app.UseRouting();

            // 4. ACTIVAR EL USO DE SESIONES
            app.UseSession();

            app.UseAuthorization();

            // Mapeo de la ruta inicial
            app.MapControllerRoute(
                name: "default",
                pattern: "{controller=Tienda}/{action=Catalogo}/{id?}");

            app.Run();
        }
    }
}