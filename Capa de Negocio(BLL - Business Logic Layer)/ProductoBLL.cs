using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LaVeguita.DAL;
using LaVeguita.Entities;

namespace LaVeguita.BLL
{
    public class ProductoBLL
    {
        private readonly ProductoDAL _productoDal = new ProductoDAL();

        public List<Producto> ListarProductos()
        {
            return _productoDal.ListarProductos();
        }

        public bool InsertarProducto(Producto p)
        {
            return _productoDal.InsertarProducto(p);
        }


        public List<TipoProducto> ListarTipos()
        {
            return _productoDal.ListarTipos();
        }
    }

}
