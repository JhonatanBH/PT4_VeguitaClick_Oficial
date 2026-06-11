namespace LaVeguita.Web
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // 1. Agregar servicios al contenedor.
            builder.Services.AddControllersWithViews();

            // 2. CONFIGURACIÓN DE SESIONES (Obligatorio para el Login)
            builder.Services.AddSession(options =>
            {
                options.IdleTimeout = TimeSpan.FromMinutes(30); // La sesión dura 30 minutos
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

            // 4. ACTIVAR EL USO DE SESIONES (Debe ir después de Routing y antes de Authorization)
            app.UseSession();

            app.UseAuthorization();

            app.MapControllerRoute(
                name: "default",
                pattern: "{controller=Tienda}/{action=Catalogo}/{id?}"); // Cambié Home por Acceso para que parta en el Login

            app.Run();
        }
    }
}
