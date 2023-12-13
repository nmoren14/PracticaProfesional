﻿//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:4.0.30319.42000
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace BancaServices.ConsultaCuentasServiceRef {
    
    
    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.ServiceModel", "4.0.0.0")]
    [System.ServiceModel.ServiceContractAttribute(Namespace="http://ws.integrator.com/", ConfigurationName="ConsultaCuentasServiceRef.ConsultarSaldoCuentas")]
    public interface ConsultarSaldoCuentas {
        
        [System.ServiceModel.OperationContractAttribute(Action="http://ws.integrator.com/ConsultarSaldoCuentas/consultarSaldoCtaByNroCuentaReques" +
            "t", ReplyAction="http://ws.integrator.com/ConsultarSaldoCuentas/consultarSaldoCtaByNroCuentaRespon" +
            "se")]
        [System.ServiceModel.XmlSerializerFormatAttribute(Style=System.ServiceModel.OperationFormatStyle.Rpc, SupportFaults=true)]
        [return: System.ServiceModel.MessageParameterAttribute(Name="return")]
        BancaServices.ConsultaCuentasServiceRef.responseConsultaSaldosCuentasBean consultarSaldoCtaByNroCuenta(string idCliente, string tipoId, string nroCuenta, string codProducto);
        
        [System.ServiceModel.OperationContractAttribute(Action="http://ws.integrator.com/ConsultarSaldoCuentas/consultarSaldoCtaByNroCuentaReques" +
            "t", ReplyAction="http://ws.integrator.com/ConsultarSaldoCuentas/consultarSaldoCtaByNroCuentaRespon" +
            "se")]
        [return: System.ServiceModel.MessageParameterAttribute(Name="return")]
        System.Threading.Tasks.Task<BancaServices.ConsultaCuentasServiceRef.responseConsultaSaldosCuentasBean> consultarSaldoCtaByNroCuentaAsync(string idCliente, string tipoId, string nroCuenta, string codProducto);
        
        [System.ServiceModel.OperationContractAttribute(Action="http://ws.integrator.com/ConsultarSaldoCuentas/consultarSaldoCtaRequest", ReplyAction="http://ws.integrator.com/ConsultarSaldoCuentas/consultarSaldoCtaResponse")]
        [System.ServiceModel.XmlSerializerFormatAttribute(Style=System.ServiceModel.OperationFormatStyle.Rpc, SupportFaults=true)]
        [return: System.ServiceModel.MessageParameterAttribute(Name="return")]
        BancaServices.ConsultaCuentasServiceRef.responseConsultaMultSaldosCuentasBean consultarSaldoCta(string idCliente, string tipoId, string codProducto);
        
        [System.ServiceModel.OperationContractAttribute(Action="http://ws.integrator.com/ConsultarSaldoCuentas/consultarSaldoCtaRequest", ReplyAction="http://ws.integrator.com/ConsultarSaldoCuentas/consultarSaldoCtaResponse")]
        [return: System.ServiceModel.MessageParameterAttribute(Name="return")]
        System.Threading.Tasks.Task<BancaServices.ConsultaCuentasServiceRef.responseConsultaMultSaldosCuentasBean> consultarSaldoCtaAsync(string idCliente, string tipoId, string codProducto);
        
        [System.ServiceModel.OperationContractAttribute(Action="http://ws.integrator.com/ConsultarSaldoCuentas/consultarSaldosCTAHRequest", ReplyAction="http://ws.integrator.com/ConsultarSaldoCuentas/consultarSaldosCTAHResponse")]
        [System.ServiceModel.XmlSerializerFormatAttribute(Style=System.ServiceModel.OperationFormatStyle.Rpc, SupportFaults=true)]
        [return: System.ServiceModel.MessageParameterAttribute(Name="return")]
        BancaServices.ConsultaCuentasServiceRef.responseConsultaSaldoCTAHBean consultarSaldosCTAH(string idCliente);
        
        [System.ServiceModel.OperationContractAttribute(Action="http://ws.integrator.com/ConsultarSaldoCuentas/consultarSaldosCTAHRequest", ReplyAction="http://ws.integrator.com/ConsultarSaldoCuentas/consultarSaldosCTAHResponse")]
        [return: System.ServiceModel.MessageParameterAttribute(Name="return")]
        System.Threading.Tasks.Task<BancaServices.ConsultaCuentasServiceRef.responseConsultaSaldoCTAHBean> consultarSaldosCTAHAsync(string idCliente);
    }
    
    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "4.7.3221.0")]
    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(Namespace="http://ws.integrator.com/")]
    public partial class responseConsultaSaldosCuentasBean : object, System.ComponentModel.INotifyPropertyChanged {
        
        private string estadoField;
        
        private string mensajeField;
        
        private consultaSaldoIVRBean cuentaField;
        
        private consultaSaldoCCIVRBean cuentaCField;
        
        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Form=System.Xml.Schema.XmlSchemaForm.Unqualified, Order=0)]
        public string estado {
            get {
                return this.estadoField;
            }
            set {
                this.estadoField = value;
                this.RaisePropertyChanged("estado");
            }
        }
        
        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Form=System.Xml.Schema.XmlSchemaForm.Unqualified, Order=1)]
        public string mensaje {
            get {
                return this.mensajeField;
            }
            set {
                this.mensajeField = value;
                this.RaisePropertyChanged("mensaje");
            }
        }
        
        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Form=System.Xml.Schema.XmlSchemaForm.Unqualified, Order=2)]
        public consultaSaldoIVRBean cuenta {
            get {
                return this.cuentaField;
            }
            set {
                this.cuentaField = value;
                this.RaisePropertyChanged("cuenta");
            }
        }
        
        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Form=System.Xml.Schema.XmlSchemaForm.Unqualified, Order=3)]
        public consultaSaldoCCIVRBean cuentaC {
            get {
                return this.cuentaCField;
            }
            set {
                this.cuentaCField = value;
                this.RaisePropertyChanged("cuentaC");
            }
        }
        
        public event System.ComponentModel.PropertyChangedEventHandler PropertyChanged;
        
        protected void RaisePropertyChanged(string propertyName) {
            System.ComponentModel.PropertyChangedEventHandler propertyChanged = this.PropertyChanged;
            if ((propertyChanged != null)) {
                propertyChanged(this, new System.ComponentModel.PropertyChangedEventArgs(propertyName));
            }
        }
    }
    
    /// <remarks/>
    [System.Xml.Serialization.XmlIncludeAttribute(typeof(consultaSaldoCCIVRBean))]
    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "4.7.3221.0")]
    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(Namespace="http://ws.integrator.com/")]
    public partial class consultaSaldoIVRBean : object, System.ComponentModel.INotifyPropertyChanged {
        
        private string estadoField;
        
        private string idClienteField;
        
        private string nroCuentaField;
        
        private string saldoCanjeField;
        
        private string saldoDispField;
        
        private string saldoMinField;
        
        private string saldoNetoField;
        
        private string saldoRetenidoField;
        
        private string saldoTotalField;
        
        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Form=System.Xml.Schema.XmlSchemaForm.Unqualified, Order=0)]
        public string estado {
            get {
                return this.estadoField;
            }
            set {
                this.estadoField = value;
                this.RaisePropertyChanged("estado");
            }
        }
        
        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Form=System.Xml.Schema.XmlSchemaForm.Unqualified, Order=1)]
        public string idCliente {
            get {
                return this.idClienteField;
            }
            set {
                this.idClienteField = value;
                this.RaisePropertyChanged("idCliente");
            }
        }
        
        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Form=System.Xml.Schema.XmlSchemaForm.Unqualified, Order=2)]
        public string nroCuenta {
            get {
                return this.nroCuentaField;
            }
            set {
                this.nroCuentaField = value;
                this.RaisePropertyChanged("nroCuenta");
            }
        }
        
        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Form=System.Xml.Schema.XmlSchemaForm.Unqualified, Order=3)]
        public string saldoCanje {
            get {
                return this.saldoCanjeField;
            }
            set {
                this.saldoCanjeField = value;
                this.RaisePropertyChanged("saldoCanje");
            }
        }
        
        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Form=System.Xml.Schema.XmlSchemaForm.Unqualified, Order=4)]
        public string saldoDisp {
            get {
                return this.saldoDispField;
            }
            set {
                this.saldoDispField = value;
                this.RaisePropertyChanged("saldoDisp");
            }
        }
        
        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Form=System.Xml.Schema.XmlSchemaForm.Unqualified, Order=5)]
        public string saldoMin {
            get {
                return this.saldoMinField;
            }
            set {
                this.saldoMinField = value;
                this.RaisePropertyChanged("saldoMin");
            }
        }
        
        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Form=System.Xml.Schema.XmlSchemaForm.Unqualified, Order=6)]
        public string saldoNeto {
            get {
                return this.saldoNetoField;
            }
            set {
                this.saldoNetoField = value;
                this.RaisePropertyChanged("saldoNeto");
            }
        }
        
        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Form=System.Xml.Schema.XmlSchemaForm.Unqualified, Order=7)]
        public string saldoRetenido {
            get {
                return this.saldoRetenidoField;
            }
            set {
                this.saldoRetenidoField = value;
                this.RaisePropertyChanged("saldoRetenido");
            }
        }
        
        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Form=System.Xml.Schema.XmlSchemaForm.Unqualified, Order=8)]
        public string saldoTotal {
            get {
                return this.saldoTotalField;
            }
            set {
                this.saldoTotalField = value;
                this.RaisePropertyChanged("saldoTotal");
            }
        }
        
        public event System.ComponentModel.PropertyChangedEventHandler PropertyChanged;
        
        protected void RaisePropertyChanged(string propertyName) {
            System.ComponentModel.PropertyChangedEventHandler propertyChanged = this.PropertyChanged;
            if ((propertyChanged != null)) {
                propertyChanged(this, new System.ComponentModel.PropertyChangedEventArgs(propertyName));
            }
        }
    }
    
    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "4.7.3221.0")]
    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(Namespace="http://ws.integrator.com/")]
    public partial class beanConsultaSaldoCTAH : object, System.ComponentModel.INotifyPropertyChanged {
        
        private string idClienteField;
        
        private string referenciaField;
        
        private string numeroCuentaField;
        
        private string saldoField;
        
        private string saldoRetenidoField;
        
        private string saldoCanjeField;
        
        private string saldoTotalField;
        
        private string fechaEmisionField;
        
        private string estadoField;
        
        private string codProductoField;
        
        private string nomProductoField;
        
        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Form=System.Xml.Schema.XmlSchemaForm.Unqualified, Order=0)]
        public string idCliente {
            get {
                return this.idClienteField;
            }
            set {
                this.idClienteField = value;
                this.RaisePropertyChanged("idCliente");
            }
        }
        
        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Form=System.Xml.Schema.XmlSchemaForm.Unqualified, Order=1)]
        public string referencia {
            get {
                return this.referenciaField;
            }
            set {
                this.referenciaField = value;
                this.RaisePropertyChanged("referencia");
            }
        }
        
        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Form=System.Xml.Schema.XmlSchemaForm.Unqualified, Order=2)]
        public string numeroCuenta {
            get {
                return this.numeroCuentaField;
            }
            set {
                this.numeroCuentaField = value;
                this.RaisePropertyChanged("numeroCuenta");
            }
        }
        
        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Form=System.Xml.Schema.XmlSchemaForm.Unqualified, Order=3)]
        public string saldo {
            get {
                return this.saldoField;
            }
            set {
                this.saldoField = value;
                this.RaisePropertyChanged("saldo");
            }
        }
        
        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Form=System.Xml.Schema.XmlSchemaForm.Unqualified, Order=4)]
        public string saldoRetenido {
            get {
                return this.saldoRetenidoField;
            }
            set {
                this.saldoRetenidoField = value;
                this.RaisePropertyChanged("saldoRetenido");
            }
        }
        
        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Form=System.Xml.Schema.XmlSchemaForm.Unqualified, Order=5)]
        public string saldoCanje {
            get {
                return this.saldoCanjeField;
            }
            set {
                this.saldoCanjeField = value;
                this.RaisePropertyChanged("saldoCanje");
            }
        }
        
        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Form=System.Xml.Schema.XmlSchemaForm.Unqualified, Order=6)]
        public string saldoTotal {
            get {
                return this.saldoTotalField;
            }
            set {
                this.saldoTotalField = value;
                this.RaisePropertyChanged("saldoTotal");
            }
        }
        
        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Form=System.Xml.Schema.XmlSchemaForm.Unqualified, Order=7)]
        public string fechaEmision {
            get {
                return this.fechaEmisionField;
            }
            set {
                this.fechaEmisionField = value;
                this.RaisePropertyChanged("fechaEmision");
            }
        }
        
        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Form=System.Xml.Schema.XmlSchemaForm.Unqualified, Order=8)]
        public string estado {
            get {
                return this.estadoField;
            }
            set {
                this.estadoField = value;
                this.RaisePropertyChanged("estado");
            }
        }
        
        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Form=System.Xml.Schema.XmlSchemaForm.Unqualified, Order=9)]
        public string codProducto {
            get {
                return this.codProductoField;
            }
            set {
                this.codProductoField = value;
                this.RaisePropertyChanged("codProducto");
            }
        }
        
        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Form=System.Xml.Schema.XmlSchemaForm.Unqualified, Order=10)]
        public string nomProducto {
            get {
                return this.nomProductoField;
            }
            set {
                this.nomProductoField = value;
                this.RaisePropertyChanged("nomProducto");
            }
        }
        
        public event System.ComponentModel.PropertyChangedEventHandler PropertyChanged;
        
        protected void RaisePropertyChanged(string propertyName) {
            System.ComponentModel.PropertyChangedEventHandler propertyChanged = this.PropertyChanged;
            if ((propertyChanged != null)) {
                propertyChanged(this, new System.ComponentModel.PropertyChangedEventArgs(propertyName));
            }
        }
    }
    
    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "4.7.3221.0")]
    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(Namespace="http://ws.integrator.com/")]
    public partial class responseConsultaSaldoCTAHBean : object, System.ComponentModel.INotifyPropertyChanged {
        
        private string estadoField;
        
        private string mensajeField;
        
        private beanConsultaSaldoCTAH[] cuentasField;
        
        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Form=System.Xml.Schema.XmlSchemaForm.Unqualified, Order=0)]
        public string estado {
            get {
                return this.estadoField;
            }
            set {
                this.estadoField = value;
                this.RaisePropertyChanged("estado");
            }
        }
        
        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Form=System.Xml.Schema.XmlSchemaForm.Unqualified, Order=1)]
        public string mensaje {
            get {
                return this.mensajeField;
            }
            set {
                this.mensajeField = value;
                this.RaisePropertyChanged("mensaje");
            }
        }
        
        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute("cuentas", Form=System.Xml.Schema.XmlSchemaForm.Unqualified, Order=2)]
        public beanConsultaSaldoCTAH[] cuentas {
            get {
                return this.cuentasField;
            }
            set {
                this.cuentasField = value;
                this.RaisePropertyChanged("cuentas");
            }
        }
        
        public event System.ComponentModel.PropertyChangedEventHandler PropertyChanged;
        
        protected void RaisePropertyChanged(string propertyName) {
            System.ComponentModel.PropertyChangedEventHandler propertyChanged = this.PropertyChanged;
            if ((propertyChanged != null)) {
                propertyChanged(this, new System.ComponentModel.PropertyChangedEventArgs(propertyName));
            }
        }
    }
    
    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "4.7.3221.0")]
    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(Namespace="http://ws.integrator.com/")]
    public partial class responseConsultaMultSaldosCuentasBean : object, System.ComponentModel.INotifyPropertyChanged {
        
        private string estadoField;
        
        private string mensajeField;
        
        private consultaSaldoIVRBean[] cuentasField;
        
        private consultaSaldoCCIVRBean[] cuentasCField;
        
        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Form=System.Xml.Schema.XmlSchemaForm.Unqualified, Order=0)]
        public string estado {
            get {
                return this.estadoField;
            }
            set {
                this.estadoField = value;
                this.RaisePropertyChanged("estado");
            }
        }
        
        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Form=System.Xml.Schema.XmlSchemaForm.Unqualified, Order=1)]
        public string mensaje {
            get {
                return this.mensajeField;
            }
            set {
                this.mensajeField = value;
                this.RaisePropertyChanged("mensaje");
            }
        }
        
        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute("cuentas", Form=System.Xml.Schema.XmlSchemaForm.Unqualified, Order=2)]
        public consultaSaldoIVRBean[] cuentas {
            get {
                return this.cuentasField;
            }
            set {
                this.cuentasField = value;
                this.RaisePropertyChanged("cuentas");
            }
        }
        
        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute("cuentasC", Form=System.Xml.Schema.XmlSchemaForm.Unqualified, Order=3)]
        public consultaSaldoCCIVRBean[] cuentasC {
            get {
                return this.cuentasCField;
            }
            set {
                this.cuentasCField = value;
                this.RaisePropertyChanged("cuentasC");
            }
        }
        
        public event System.ComponentModel.PropertyChangedEventHandler PropertyChanged;
        
        protected void RaisePropertyChanged(string propertyName) {
            System.ComponentModel.PropertyChangedEventHandler propertyChanged = this.PropertyChanged;
            if ((propertyChanged != null)) {
                propertyChanged(this, new System.ComponentModel.PropertyChangedEventArgs(propertyName));
            }
        }
    }
    
    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "4.7.3221.0")]
    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(Namespace="http://ws.integrator.com/")]
    public partial class consultaSaldoCCIVRBean : consultaSaldoIVRBean {
        
        private string dispSobregiroField;
        
        private string limiteSobregiroField;
        
        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Form=System.Xml.Schema.XmlSchemaForm.Unqualified, Order=0)]
        public string dispSobregiro {
            get {
                return this.dispSobregiroField;
            }
            set {
                this.dispSobregiroField = value;
                this.RaisePropertyChanged("dispSobregiro");
            }
        }
        
        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Form=System.Xml.Schema.XmlSchemaForm.Unqualified, Order=1)]
        public string limiteSobregiro {
            get {
                return this.limiteSobregiroField;
            }
            set {
                this.limiteSobregiroField = value;
                this.RaisePropertyChanged("limiteSobregiro");
            }
        }
    }
    
    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.ServiceModel", "4.0.0.0")]
    public interface ConsultarSaldoCuentasChannel : BancaServices.ConsultaCuentasServiceRef.ConsultarSaldoCuentas, System.ServiceModel.IClientChannel {
    }
    
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.ServiceModel", "4.0.0.0")]
    public partial class ConsultarSaldoCuentasClient : System.ServiceModel.ClientBase<BancaServices.ConsultaCuentasServiceRef.ConsultarSaldoCuentas>, BancaServices.ConsultaCuentasServiceRef.ConsultarSaldoCuentas {
        
        public ConsultarSaldoCuentasClient() {
        }
        
        public ConsultarSaldoCuentasClient(string endpointConfigurationName) : 
                base(endpointConfigurationName) {
        }
        
        public ConsultarSaldoCuentasClient(string endpointConfigurationName, string remoteAddress) : 
                base(endpointConfigurationName, remoteAddress) {
        }
        
        public ConsultarSaldoCuentasClient(string endpointConfigurationName, System.ServiceModel.EndpointAddress remoteAddress) : 
                base(endpointConfigurationName, remoteAddress) {
        }
        
        public ConsultarSaldoCuentasClient(System.ServiceModel.Channels.Binding binding, System.ServiceModel.EndpointAddress remoteAddress) : 
                base(binding, remoteAddress) {
        }
        
        public BancaServices.ConsultaCuentasServiceRef.responseConsultaSaldosCuentasBean consultarSaldoCtaByNroCuenta(string idCliente, string tipoId, string nroCuenta, string codProducto) {
            return base.Channel.consultarSaldoCtaByNroCuenta(idCliente, tipoId, nroCuenta, codProducto);
        }
        
        public System.Threading.Tasks.Task<BancaServices.ConsultaCuentasServiceRef.responseConsultaSaldosCuentasBean> consultarSaldoCtaByNroCuentaAsync(string idCliente, string tipoId, string nroCuenta, string codProducto) {
            return base.Channel.consultarSaldoCtaByNroCuentaAsync(idCliente, tipoId, nroCuenta, codProducto);
        }
        
        public BancaServices.ConsultaCuentasServiceRef.responseConsultaMultSaldosCuentasBean consultarSaldoCta(string idCliente, string tipoId, string codProducto) {
            return base.Channel.consultarSaldoCta(idCliente, tipoId, codProducto);
        }
        
        public System.Threading.Tasks.Task<BancaServices.ConsultaCuentasServiceRef.responseConsultaMultSaldosCuentasBean> consultarSaldoCtaAsync(string idCliente, string tipoId, string codProducto) {
            return base.Channel.consultarSaldoCtaAsync(idCliente, tipoId, codProducto);
        }
        
        public BancaServices.ConsultaCuentasServiceRef.responseConsultaSaldoCTAHBean consultarSaldosCTAH(string idCliente) {
            return base.Channel.consultarSaldosCTAH(idCliente);
        }
        
        public System.Threading.Tasks.Task<BancaServices.ConsultaCuentasServiceRef.responseConsultaSaldoCTAHBean> consultarSaldosCTAHAsync(string idCliente) {
            return base.Channel.consultarSaldosCTAHAsync(idCliente);
        }
    }
}
