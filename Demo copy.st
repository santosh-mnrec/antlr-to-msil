using System.Collections.Generic;
using System.Linq;
 
namespace $Model.Namespace$
{
   public class $Model.Name$ : Demo.ViewModelBase<$Model.ModelTypeName$>
   {
   $Model.Properties:
   {
      property|
      public $property.TypeName$ $property.Name$
      {      
      $if(property.CanGet)$
         get
         { 
            return Model.$property.Name$; 
         \} 
      $endif$
                
      $if(property.CanSet)$
         set
         { 
            if(Model.$property.Name$ == value) return;
            Model.$property.Name$ = value; 
            OnPropertyChanged("$property.Name$");
         \} 
      $endif$         
      \}
   }$ 
    
   $Model.ViewModelProperties:
   {
      property|
      private $property.TypeName$ _$property.Name$;
      public $property.TypeName$ $property.Name$
      {
      $if(property.CanGet)$
         get
         { 
            if(_$property.Name$ == null) _$property.Name$ = new $property.TypeName$(Model.$property.Name$);
            return _$property.Name$; 
         \} 
      $endif$
             
      $if(property.CanSet)$
         set
         { 
            if(_$property.Name$ == value) return;
            _$property.Name$ = value; 
            Model.$property.Name$ = value.Model; 
            OnPropertyChanged("$property.Name$");
         \} 
      $endif$
      \}
   }$
    
   $Model.ViewModelListProperties:
   {
      property|
      private List<$property.TypeName$> _$property.Name$;
      public List<$property.TypeName$> $property.Name$
      {
         get
         { 
            if(_$property.Name$ == null) _$property.Name$ = new List<$property.TypeName$>(Model.$property.Name$.Select(m => new $property.TypeName$(m)));
            return _$property.Name$; 
         \} 
      \}
   }$
      public $Model.Name$($Model.ModelTypeName$ model) : base(model)
      {
      }
   }
}