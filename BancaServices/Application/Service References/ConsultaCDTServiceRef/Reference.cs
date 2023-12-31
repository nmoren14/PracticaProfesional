﻿//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:4.0.30319.42000
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace BancaServices.ConsultaCDTServiceRef {
    
    
    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.ServiceModel", "4.0.0.0")]
    [System.ServiceModel.ServiceContractAttribute(Namespace="http://ws.integrator.com/", ConfigurationName="ConsultaCDTServiceRef.ConsultarCDT")]
    public interface ConsultarCDT {
        
        // CODEGEN: Parameter 'return' requires additional schema information that cannot be captured using the parameter mode. The specific attribute is 'System.Xml.Serialization.XmlElementAttribute'.
        [System.ServiceModel.OperationContractAttribute(Action="http://ws.integrator.com/ConsultarCDT/consultarCDTRequest", ReplyAction="http://ws.integrator.com/ConsultarCDT/consultarCDTResponse")]
        [System.ServiceModel.XmlSerializerFormatAttribute(SupportFaults=true)]
        [return: System.ServiceModel.MessageParameterAttribute(Name="return")]
        BancaServices.ConsultaCDTServiceRef.consultarCDTResponse consultarCDT(BancaServices.ConsultaCDTServiceRef.consultarCDTRequest request);
        
        [System.ServiceModel.OperationContractAttribute(Action="http://ws.integrator.com/ConsultarCDT/consultarCDTRequest", ReplyAction="http://ws.integrator.com/ConsultarCDT/consultarCDTResponse")]
        System.Threading.Tasks.Task<BancaServices.ConsultaCDTServiceRef.consultarCDTResponse> consultarCDTAsync(BancaServices.ConsultaCDTServiceRef.consultarCDTRequest request);
        
        // CODEGEN: Parameter 'return' requires additional schema information that cannot be captured using the parameter mode. The specific attribute is 'System.Xml.Serialization.XmlElementAttribute'.
        [System.ServiceModel.OperationContractAttribute(Action="http://ws.integrator.com/ConsultarCDT/consultarDetalleCDTRequest", ReplyAction="http://ws.integrator.com/ConsultarCDT/consultarDetalleCDTResponse")]
        [System.ServiceModel.XmlSerializerFormatAttribute(SupportFaults=true)]
        [return: System.ServiceModel.MessageParameterAttribute(Name="return")]
        BancaServices.ConsultaCDTServiceRef.consultarDetalleCDTResponse consultarDetalleCDT(BancaServices.ConsultaCDTServiceRef.consultarDetalleCDTRequest request);
        
        [System.ServiceModel.OperationContractAttribute(Action="http://ws.integrator.com/ConsultarCDT/consultarDetalleCDTRequest", ReplyAction="http://ws.integrator.com/ConsultarCDT/consultarDetalleCDTResponse")]
        System.Threading.Tasks.Task<BancaServices.ConsultaCDTServiceRef.consultarDetalleCDTResponse> consultarDetalleCDTAsync(BancaServices.ConsultaCDTServiceRef.consultarDetalleCDTRequest request);
    }
    
    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "4.7.3221.0")]
    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(Namespace="http://ws.integrator.com/")]
    public partial class responseConsultaSaldoCDTBean : object, System.ComponentModel.INotifyPropertyChanged {
        
        private string estadoField;
        
        private string mensajeField;
        
        private beanConsultaSaldoCDT[] cuentasField;
        
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
        public beanConsultaSaldoCDT[] cuentas {
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
    public partial class beanConsultaSaldoCDT : object, System.ComponentModel.INotifyPropertyChanged {
        
        private string idClienteField;
        
        private string referenciaField;
        
        private string numeroCuentaField;
        
        private string saldoField;
        
        private string fechaEmisionField;
        
        private string estadoField;
        
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
        [System.Xml.Serialization.XmlElementAttribute(Form=System.Xml.Schema.XmlSchemaForm.Unqualified, Order=5)]
        public string estado {
            get {
                return this.estadoField;
            }
            set {
                this.estadoField = value;
                this.RaisePropertyChanged("estado");
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
    public partial class beanConsultaDetalleCDT : object, System.ComponentModel.INotifyPropertyChanged {
        
        private string valorCDTField;
        
        private string duracionField;
        
        private string frecuenciaPagoInteresField;
        
        private string tasaNominalField;
        
        private string valorInteresField;
        
        private string valorInteresNetoField;
        
        private string fechaVencimientoField;
        
        private string modalidadInteresField;
        
        private string proximaCuotaIneteresField;
        
        private string tasaEfectivaField;
        
        private string valorRetencionField;
        
        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Form=System.Xml.Schema.XmlSchemaForm.Unqualified, Order=0)]
        public string valorCDT {
            get {
                return this.valorCDTField;
            }
            set {
                this.valorCDTField = value;
                this.RaisePropertyChanged("valorCDT");
            }
        }
        
        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Form=System.Xml.Schema.XmlSchemaForm.Unqualified, Order=1)]
        public string duracion {
            get {
                return this.duracionField;
            }
            set {
                this.duracionField = value;
                this.RaisePropertyChanged("duracion");
            }
        }
        
        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Form=System.Xml.Schema.XmlSchemaForm.Unqualified, Order=2)]
        public string frecuenciaPagoInteres {
            get {
                return this.frecuenciaPagoInteresField;
            }
            set {
                this.frecuenciaPagoInteresField = value;
                this.RaisePropertyChanged("frecuenciaPagoInteres");
            }
        }
        
        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Form=System.Xml.Schema.XmlSchemaForm.Unqualified, Order=3)]
        public string tasaNominal {
            get {
                return this.tasaNominalField;
            }
            set {
                this.tasaNominalField = value;
                this.RaisePropertyChanged("tasaNominal");
            }
        }
        
        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Form=System.Xml.Schema.XmlSchemaForm.Unqualified, Order=4)]
        public string valorInteres {
            get {
                return this.valorInteresField;
            }
            set {
                this.valorInteresField = value;
                this.RaisePropertyChanged("valorInteres");
            }
        }
        
        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Form=System.Xml.Schema.XmlSchemaForm.Unqualified, Order=5)]
        public string valorInteresNeto {
            get {
                return this.valorInteresNetoField;
            }
            set {
                this.valorInteresNetoField = value;
                this.RaisePropertyChanged("valorInteresNeto");
            }
        }
        
        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Form=System.Xml.Schema.XmlSchemaForm.Unqualified, Order=6)]
        public string fechaVencimiento {
            get {
                return this.fechaVencimientoField;
            }
            set {
                this.fechaVencimientoField = value;
                this.RaisePropertyChanged("fechaVencimiento");
            }
        }
        
        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Form=System.Xml.Schema.XmlSchemaForm.Unqualified, Order=7)]
        public string modalidadInteres {
            get {
                return this.modalidadInteresField;
            }
            set {
                this.modalidadInteresField = value;
                this.RaisePropertyChanged("modalidadInteres");
            }
        }
        
        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Form=System.Xml.Schema.XmlSchemaForm.Unqualified, Order=8)]
        public string proximaCuotaIneteres {
            get {
                return this.proximaCuotaIneteresField;
            }
            set {
                this.proximaCuotaIneteresField = value;
                this.RaisePropertyChanged("proximaCuotaIneteres");
            }
        }
        
        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Form=System.Xml.Schema.XmlSchemaForm.Unqualified, Order=9)]
        public string tasaEfectiva {
            get {
                return this.tasaEfectivaField;
            }
            set {
                this.tasaEfectivaField = value;
                this.RaisePropertyChanged("tasaEfectiva");
            }
        }
        
        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Form=System.Xml.Schema.XmlSchemaForm.Unqualified, Order=10)]
        public string valorRetencion {
            get {
                return this.valorRetencionField;
            }
            set {
                this.valorRetencionField = value;
                this.RaisePropertyChanged("valorRetencion");
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
    public partial class responseConsultaDetalleCDTBean : object, System.ComponentModel.INotifyPropertyChanged {
        
        private string estadoField;
        
        private string mensajeField;
        
        private beanConsultaDetalleCDT[] cuentasField;
        
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
        public beanConsultaDetalleCDT[] cuentas {
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
    
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.ServiceModel", "4.0.0.0")]
    [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Advanced)]
    [System.ServiceModel.MessageContractAttribute(WrapperName="consultarCDT", WrapperNamespace="http://ws.integrator.com/", IsWrapped=true)]
    public partial class consultarCDTRequest {
        
        [System.ServiceModel.MessageBodyMemberAttribute(Namespace="http://ws.integrator.com/", Order=0)]
        [System.Xml.Serialization.XmlElementAttribute(Form=System.Xml.Schema.XmlSchemaForm.Unqualified)]
        public string idClienete;
        
        public consultarCDTRequest() {
        }
        
        public consultarCDTRequest(string idClienete) {
            this.idClienete = idClienete;
        }
    }
    
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.ServiceModel", "4.0.0.0")]
    [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Advanced)]
    [System.ServiceModel.MessageContractAttribute(WrapperName="consultarCDTResponse", WrapperNamespace="http://ws.integrator.com/", IsWrapped=true)]
    public partial class consultarCDTResponse {
        
        [System.ServiceModel.MessageBodyMemberAttribute(Namespace="http://ws.integrator.com/", Order=0)]
        [System.Xml.Serialization.XmlElementAttribute(Form=System.Xml.Schema.XmlSchemaForm.Unqualified)]
        public BancaServices.ConsultaCDTServiceRef.responseConsultaSaldoCDTBean @return;
        
        public consultarCDTResponse() {
        }
        
        public consultarCDTResponse(BancaServices.ConsultaCDTServiceRef.responseConsultaSaldoCDTBean @return) {
            this.@return = @return;
        }
    }
    
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.ServiceModel", "4.0.0.0")]
    [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Advanced)]
    [System.ServiceModel.MessageContractAttribute(WrapperName="consultarDetalleCDT", WrapperNamespace="http://ws.integrator.com/", IsWrapped=true)]
    public partial class consultarDetalleCDTRequest {
        
        [System.ServiceModel.MessageBodyMemberAttribute(Namespace="http://ws.integrator.com/", Order=0)]
        [System.Xml.Serialization.XmlElementAttribute(Form=System.Xml.Schema.XmlSchemaForm.Unqualified)]
        public string numProducto;
        
        public consultarDetalleCDTRequest() {
        }
        
        public consultarDetalleCDTRequest(string numProducto) {
            this.numProducto = numProducto;
        }
    }
    
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.ServiceModel", "4.0.0.0")]
    [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Advanced)]
    [System.ServiceModel.MessageContractAttribute(WrapperName="consultarDetalleCDTResponse", WrapperNamespace="http://ws.integrator.com/", IsWrapped=true)]
    public partial class consultarDetalleCDTResponse {
        
        [System.ServiceModel.MessageBodyMemberAttribute(Namespace="http://ws.integrator.com/", Order=0)]
        [System.Xml.Serialization.XmlElementAttribute(Form=System.Xml.Schema.XmlSchemaForm.Unqualified)]
        public BancaServices.ConsultaCDTServiceRef.responseConsultaDetalleCDTBean @return;
        
        public consultarDetalleCDTResponse() {
        }
        
        public consultarDetalleCDTResponse(BancaServices.ConsultaCDTServiceRef.responseConsultaDetalleCDTBean @return) {
            this.@return = @return;
        }
    }
    
    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.ServiceModel", "4.0.0.0")]
    public interface ConsultarCDTChannel : BancaServices.ConsultaCDTServiceRef.ConsultarCDT, System.ServiceModel.IClientChannel {
    }
    
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.ServiceModel", "4.0.0.0")]
    public partial class ConsultarCDTClient : System.ServiceModel.ClientBase<BancaServices.ConsultaCDTServiceRef.ConsultarCDT>, BancaServices.ConsultaCDTServiceRef.ConsultarCDT {
        
        public ConsultarCDTClient() {
        }
        
        public ConsultarCDTClient(string endpointConfigurationName) : 
                base(endpointConfigurationName) {
        }
        
        public ConsultarCDTClient(string endpointConfigurationName, string remoteAddress) : 
                base(endpointConfigurationName, "http://10.231.50.185:8082/Genesys/ConsultarCDT") {
        }
        
        public ConsultarCDTClient(string endpointConfigurationName, System.ServiceModel.EndpointAddress remoteAddress) : 
                base(endpointConfigurationName, remoteAddress) {
        }
        
        public ConsultarCDTClient(System.ServiceModel.Channels.Binding binding, System.ServiceModel.EndpointAddress remoteAddress) : 
                base(binding, remoteAddress) {
        }
        
        [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Advanced)]
        BancaServices.ConsultaCDTServiceRef.consultarCDTResponse BancaServices.ConsultaCDTServiceRef.ConsultarCDT.consultarCDT(BancaServices.ConsultaCDTServiceRef.consultarCDTRequest request) {
            return base.Channel.consultarCDT(request);
        }
        
        public BancaServices.ConsultaCDTServiceRef.responseConsultaSaldoCDTBean consultarCDT(string idClienete) {
            BancaServices.ConsultaCDTServiceRef.consultarCDTRequest inValue = new BancaServices.ConsultaCDTServiceRef.consultarCDTRequest();
            inValue.idClienete = idClienete;
            BancaServices.ConsultaCDTServiceRef.consultarCDTResponse retVal = ((BancaServices.ConsultaCDTServiceRef.ConsultarCDT)(this)).consultarCDT(inValue);
            return retVal.@return;
        }
        
        [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Advanced)]
        System.Threading.Tasks.Task<BancaServices.ConsultaCDTServiceRef.consultarCDTResponse> BancaServices.ConsultaCDTServiceRef.ConsultarCDT.consultarCDTAsync(BancaServices.ConsultaCDTServiceRef.consultarCDTRequest request) {
            return base.Channel.consultarCDTAsync(request);
        }
        
        public System.Threading.Tasks.Task<BancaServices.ConsultaCDTServiceRef.consultarCDTResponse> consultarCDTAsync(string idClienete) {
            BancaServices.ConsultaCDTServiceRef.consultarCDTRequest inValue = new BancaServices.ConsultaCDTServiceRef.consultarCDTRequest();
            inValue.idClienete = idClienete;
            return ((BancaServices.ConsultaCDTServiceRef.ConsultarCDT)(this)).consultarCDTAsync(inValue);
        }
        
        [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Advanced)]
        BancaServices.ConsultaCDTServiceRef.consultarDetalleCDTResponse BancaServices.ConsultaCDTServiceRef.ConsultarCDT.consultarDetalleCDT(BancaServices.ConsultaCDTServiceRef.consultarDetalleCDTRequest request) {
            return base.Channel.consultarDetalleCDT(request);
        }
        
        public BancaServices.ConsultaCDTServiceRef.responseConsultaDetalleCDTBean consultarDetalleCDT(string numProducto) {
            BancaServices.ConsultaCDTServiceRef.consultarDetalleCDTRequest inValue = new BancaServices.ConsultaCDTServiceRef.consultarDetalleCDTRequest();
            inValue.numProducto = numProducto;
            BancaServices.ConsultaCDTServiceRef.consultarDetalleCDTResponse retVal = ((BancaServices.ConsultaCDTServiceRef.ConsultarCDT)(this)).consultarDetalleCDT(inValue);
            return retVal.@return;
        }
        
        [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Advanced)]
        System.Threading.Tasks.Task<BancaServices.ConsultaCDTServiceRef.consultarDetalleCDTResponse> BancaServices.ConsultaCDTServiceRef.ConsultarCDT.consultarDetalleCDTAsync(BancaServices.ConsultaCDTServiceRef.consultarDetalleCDTRequest request) {
            return base.Channel.consultarDetalleCDTAsync(request);
        }
        
        public System.Threading.Tasks.Task<BancaServices.ConsultaCDTServiceRef.consultarDetalleCDTResponse> consultarDetalleCDTAsync(string numProducto) {
            BancaServices.ConsultaCDTServiceRef.consultarDetalleCDTRequest inValue = new BancaServices.ConsultaCDTServiceRef.consultarDetalleCDTRequest();
            inValue.numProducto = numProducto;
            return ((BancaServices.ConsultaCDTServiceRef.ConsultarCDT)(this)).consultarDetalleCDTAsync(inValue);
        }
    }
}
