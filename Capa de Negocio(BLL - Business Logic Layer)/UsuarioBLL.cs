using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LaVeguita.DAL;
using LaVeguita.Entities;

namespace LaVeguita.BLL
{
    public class UsuarioBLL
    {
        private UsuarioDAL dal = new UsuarioDAL();

        // 1. LISTAR
        public List<Usuario> ObtenerTodos()
        {
            return dal.ListarUsuarios();
        }

        // 2. AGREGAR (El que usa tu Controlador)
        public bool AgregarUsuario(Usuario u)
        {
            if (string.IsNullOrEmpty(u.NombreUser)) return false;
            u.IdRolUsuario = 1; // Este ID 1 existe en tu tabla ROL (Administrador)
            u.IdDireccion = 100; // Por si acaso el HTML falla, asegúralo aquí también
            return dal.InsertarUsuario(u);
        }

        // 3. EDITAR
        public bool ActualizarUsuario(Usuario u)
        {
            if (u.IdUsuario <= 0 || string.IsNullOrEmpty(u.NombreUser)) return false;
            return dal.ActualizarUsuario(u);
        }

        public Usuario ObtenerPorId(int id)
        {
            // Simplemente llama a la DAL
            return dal.ObtenerPorId(id);
        }


        public List<Rol> ListarRoles()
        {
            return dal.ListarRoles(); // Llama a la función que pusimos en la DAL
        }


        // 4. BORRAR (El que usa tu Controlador)
        public bool BorrarUsuario(int id)
        {
            if (id <= 0) return false;
            return dal.EliminarUsuario(id);
        }

        /* NOTA: He quitado los métodos "Administrador" para evitar código basura. 
           Tu Controlador ahora debe llamar siempre a:
           - AgregarUsuario
           - BorrarUsuario
           - ActualizarUsuario
        */
    }
}
