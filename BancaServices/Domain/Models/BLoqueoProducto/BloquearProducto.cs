namespace BancaServices.Models.BLoqueoProducto
{
    public class BloquearProducto
    {
        public string tipoId { get; set; }
        public string idCliente { get; set; }
        public string numProducto { get; set; }
        public string tipoProducto { get; set; }
        public string productoBloqueo { get; set; }
        public string tipoBloqueo { get; set; }

        public bool validateAttribute()
        {
            bool result = false;
            if (!string.IsNullOrEmpty(this.idCliente) && !string.IsNullOrEmpty(this.tipoId) && !string.IsNullOrEmpty(this.numProducto) && !string.IsNullOrEmpty(this.tipoProducto) && !string.IsNullOrEmpty(this.productoBloqueo) && !string.IsNullOrEmpty(this.tipoBloqueo))
            {
                result = true;
            }
            return result;
        }
    }


}