﻿//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:4.0.30319.18408
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace JetterPilotConsoleTest.JetterCommServiceReference {
    using System.Runtime.Serialization;
    using System;
    
    
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.Runtime.Serialization", "4.0.0.0")]
    [System.Runtime.Serialization.DataContractAttribute(Name="PilotCommand", Namespace="http://schemas.datacontract.org/2004/07/JetterCommServiceLibrary")]
    [System.SerializableAttribute()]
    public partial class PilotCommand : object, System.Runtime.Serialization.IExtensibleDataObject, System.ComponentModel.INotifyPropertyChanged {
        
        [System.NonSerializedAttribute()]
        private System.Runtime.Serialization.ExtensionDataObject extensionDataField;
        
        [System.Runtime.Serialization.OptionalFieldAttribute()]
        private JetterPilotConsoleTest.JetterCommServiceReference.PilotCommandType commandField;
        
        [global::System.ComponentModel.BrowsableAttribute(false)]
        public System.Runtime.Serialization.ExtensionDataObject ExtensionData {
            get {
                return this.extensionDataField;
            }
            set {
                this.extensionDataField = value;
            }
        }
        
        [System.Runtime.Serialization.DataMemberAttribute()]
        public JetterPilotConsoleTest.JetterCommServiceReference.PilotCommandType command {
            get {
                return this.commandField;
            }
            set {
                if ((this.commandField.Equals(value) != true)) {
                    this.commandField = value;
                    this.RaisePropertyChanged("command");
                }
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
    
    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.Runtime.Serialization", "4.0.0.0")]
    [System.Runtime.Serialization.DataContractAttribute(Name="PilotCommandType", Namespace="http://schemas.datacontract.org/2004/07/JetterCommServiceLibrary")]
    public enum PilotCommandType : int {
        
        [System.Runtime.Serialization.EnumMemberAttribute()]
        RudderNeutral = 0,
        
        [System.Runtime.Serialization.EnumMemberAttribute()]
        RudderRight = 1,
        
        [System.Runtime.Serialization.EnumMemberAttribute()]
        RudderLeft = 2,
        
        [System.Runtime.Serialization.EnumMemberAttribute()]
        ThrustNeutral = 3,
        
        [System.Runtime.Serialization.EnumMemberAttribute()]
        ThrustUp = 4,
        
        [System.Runtime.Serialization.EnumMemberAttribute()]
        ThrustDown = 5,
    }
    
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.Runtime.Serialization", "4.0.0.0")]
    [System.Runtime.Serialization.DataContractAttribute(Name="CommPointInfo", Namespace="http://schemas.datacontract.org/2004/07/JetterCommServiceLibrary")]
    [System.SerializableAttribute()]
    public partial class CommPointInfo : object, System.Runtime.Serialization.IExtensibleDataObject, System.ComponentModel.INotifyPropertyChanged {
        
        [System.NonSerializedAttribute()]
        private System.Runtime.Serialization.ExtensionDataObject extensionDataField;
        
        [System.Runtime.Serialization.OptionalFieldAttribute()]
        private string nameField;
        
        [global::System.ComponentModel.BrowsableAttribute(false)]
        public System.Runtime.Serialization.ExtensionDataObject ExtensionData {
            get {
                return this.extensionDataField;
            }
            set {
                this.extensionDataField = value;
            }
        }
        
        [System.Runtime.Serialization.DataMemberAttribute()]
        public string name {
            get {
                return this.nameField;
            }
            set {
                if ((object.ReferenceEquals(this.nameField, value) != true)) {
                    this.nameField = value;
                    this.RaisePropertyChanged("name");
                }
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
    
    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.ServiceModel", "4.0.0.0")]
    [System.ServiceModel.ServiceContractAttribute(ConfigurationName="JetterCommServiceReference.IJetterCommService", CallbackContract=typeof(JetterPilotConsoleTest.JetterCommServiceReference.IJetterCommServiceCallback), SessionMode=System.ServiceModel.SessionMode.Required)]
    public interface IJetterCommService {
        
        [System.ServiceModel.OperationContractAttribute(IsOneWay=true, IsInitiating=false, Action="http://tempuri.org/IJetterCommService/PilotRequest")]
        void PilotRequest(JetterPilotConsoleTest.JetterCommServiceReference.PilotCommand pilot_command);
        
        [System.ServiceModel.OperationContractAttribute(IsOneWay=true, IsInitiating=false, Action="http://tempuri.org/IJetterCommService/Say")]
        void Say(string msg);
        
        [System.ServiceModel.OperationContractAttribute(IsOneWay=true, IsInitiating=false, Action="http://tempuri.org/IJetterCommService/Whisper")]
        void Whisper(string to, string msg);
        
        [System.ServiceModel.OperationContractAttribute(Action="http://tempuri.org/IJetterCommService/Join", ReplyAction="http://tempuri.org/IJetterCommService/JoinResponse")]
        JetterPilotConsoleTest.JetterCommServiceReference.CommPointInfo[] Join(string pilot_name);
        
        [System.ServiceModel.OperationContractAttribute(IsOneWay=true, IsTerminating=true, IsInitiating=false, Action="http://tempuri.org/IJetterCommService/Leave")]
        void Leave();
    }
    
    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.ServiceModel", "4.0.0.0")]
    public interface IJetterCommServiceCallback {
        
        [System.ServiceModel.OperationContractAttribute(IsOneWay=true, Action="http://tempuri.org/IJetterCommService/Receive")]
        void Receive(string sender, string message);
        
        [System.ServiceModel.OperationContractAttribute(IsOneWay=true, Action="http://tempuri.org/IJetterCommService/ReceiveWhisper")]
        void ReceiveWhisper(string sender, string message);
        
        [System.ServiceModel.OperationContractAttribute(IsOneWay=true, Action="http://tempuri.org/IJetterCommService/ReceiveServerStatusMessage")]
        void ReceiveServerStatusMessage(string message);
        
        [System.ServiceModel.OperationContractAttribute(IsOneWay=true, Action="http://tempuri.org/IJetterCommService/CommPointEnter")]
        void CommPointEnter(JetterPilotConsoleTest.JetterCommServiceReference.CommPointInfo comm_point);
        
        [System.ServiceModel.OperationContractAttribute(IsOneWay=true, Action="http://tempuri.org/IJetterCommService/CommPointLeave")]
        void CommPointLeave(string comm_point_name);
    }
    
    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.ServiceModel", "4.0.0.0")]
    public interface IJetterCommServiceChannel : JetterPilotConsoleTest.JetterCommServiceReference.IJetterCommService, System.ServiceModel.IClientChannel {
    }
    
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.ServiceModel", "4.0.0.0")]
    public partial class JetterCommServiceClient : System.ServiceModel.DuplexClientBase<JetterPilotConsoleTest.JetterCommServiceReference.IJetterCommService>, JetterPilotConsoleTest.JetterCommServiceReference.IJetterCommService {
        
        public JetterCommServiceClient(System.ServiceModel.InstanceContext callbackInstance) : 
                base(callbackInstance) {
        }
        
        public JetterCommServiceClient(System.ServiceModel.InstanceContext callbackInstance, string endpointConfigurationName) : 
                base(callbackInstance, endpointConfigurationName) {
        }
        
        public JetterCommServiceClient(System.ServiceModel.InstanceContext callbackInstance, string endpointConfigurationName, string remoteAddress) : 
                base(callbackInstance, endpointConfigurationName, remoteAddress) {
        }
        
        public JetterCommServiceClient(System.ServiceModel.InstanceContext callbackInstance, string endpointConfigurationName, System.ServiceModel.EndpointAddress remoteAddress) : 
                base(callbackInstance, endpointConfigurationName, remoteAddress) {
        }
        
        public JetterCommServiceClient(System.ServiceModel.InstanceContext callbackInstance, System.ServiceModel.Channels.Binding binding, System.ServiceModel.EndpointAddress remoteAddress) : 
                base(callbackInstance, binding, remoteAddress) {
        }
        
        public void PilotRequest(JetterPilotConsoleTest.JetterCommServiceReference.PilotCommand pilot_command) {
            base.Channel.PilotRequest(pilot_command);
        }
        
        public void Say(string msg) {
            base.Channel.Say(msg);
        }
        
        public void Whisper(string to, string msg) {
            base.Channel.Whisper(to, msg);
        }
        
        public JetterPilotConsoleTest.JetterCommServiceReference.CommPointInfo[] Join(string pilot_name) {
            return base.Channel.Join(pilot_name);
        }
        
        public void Leave() {
            base.Channel.Leave();
        }
    }
}
