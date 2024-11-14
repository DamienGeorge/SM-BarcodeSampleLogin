//using System.Collections;
//using Thermo.SampleManager.Common;
//using Thermo.SampleManager.Common.Data;
//using Thermo.SampleManager.Server;
//using Thermo.SampleManager.Server.Workflow.Attributes;
//using Thermo.SampleManager.Server.Workflow.Definition;
//using Thermo.SampleManager.Server.Workflow.Helpers;
//using Thermo.SampleManager.Server.Workflow.Nodes;

//namespace Thermo.SampleManager.Workflow
//{
//    /// <summary>
//    /// Bag View Node
//    /// </summary>
//    [WorkflowNode(NodeType, "NodeIfAnyFormulaName", "NodeIfAnyFormulaCategory", "CHECKS", "NodeIfAnyFormulaDescription", "WorkflowMessages")]
//    [Tag("CONDITION")]
//    [FollowsTag("DATA")]
//    [FollowsTag("CONDITION_SET")]
//    public class IfAnyFormulaNode : Node
//    {
//        #region Constants

//        /// <summary>
//        /// The workflow node type this node implements
//        /// </summary>
//        public const string NodeType = "IF_ANY_FORMULA";
//        private IEntity m_Entity;
//        private TableRelationshipMapping m_PropertyRelationshipMapping;
//        #endregion

//        #region Constructor

//        /// <summary>
//        /// Initializes a new instance of the <see cref="MessageBoxNode"/> class.
//        /// </summary>
//        /// <param name="node"></param>
//        public IfAnyFormulaNode(WorkflowNodeInternal node) : base(node)
//        {
//        }

//        #endregion

//        #region Parameters
//        [DataSourceParameter("ENTITY_NAME", "NodeChildMatchConditionParameterEntity", "NodeChildMatchConditionParameterEntityDescription")]
//        public string EntityName
//        {
//            get => this.GetParameterBagValue<string>("ENTITY_NAME");
//            set => this.SetParameterBagValue("ENTITY_NAME", (object)value);
//        }

//        [LinkedEntityTypePropertyParameter("CHILDREN_PROPERTY", "NodeChildMatchConditionParameterChildren", "NodeChildMatchConditionParameterChildrenDescription", "ENTITY_NAME", PropertyFilter = PropertyFilter.Collection)]
//        public string ChildrenProperty
//        {
//            get => this.GetParameterBagValue<string>("CHILDREN_PROPERTY");
//            set => this.SetParameterBagValue("CHILDREN_PROPERTY", (object)value);
//        }

//        [ChildEntityTypePropertyParameter("PROPERTY", "NodeChildMatchConditionParameterProperty", "NodeChildMatchConditionParameterPropertyDescription", "ENTITY_NAME", "CHILDREN_PROPERTY", PropertyFilter = PropertyFilter.NonCollection)]
//        public string Property
//        {
//            get => this.GetParameterBagValue<string>("PROPERTY");
//            set => this.SetParameterBagValue("PROPERTY", (object)value);
//        }

//        [ChildPropertyConditionOperatorParameter("OPERATOR", "NodeChildMatchConditionParameterOperator", "NodeChildMatchConditionParameterOperatorDescription", "ENTITY_NAME", "PROPERTY", "CHILDREN_PROPERTY")]
//        public string Operator
//        {
//            get => this.GetParameterBagValue<string>("OPERATOR", "=");
//            set => this.SetParameterBagValue("OPERATOR", (object)value);
//        }

//        [ChildPropertyConditionValueParameter("VALUE", "NodeChildMatchConditionParameterValue", "NodeChildMatchConditionParameterValueDescription", "ENTITY_NAME", "PROPERTY", "OPERATOR", "CHILDREN_PROPERTY")]
//        public string Value
//        {
//            get => this.GetParameterBagValue<string>("VALUE");
//            set => this.SetParameterBagValue("VALUE", value);
//        }

//        protected IEntity Entity
//        {
//            get
//            {
//                if (this.m_Entity == null)
//                    this.m_Entity = this.GetDataSourceEntity(this.EntityName);
//                return this.m_Entity;
//            }
//        }

//        protected TableRelationshipMapping PropertyRelationshipMapping
//        {
//            get
//            {
//                if (this.m_PropertyRelationshipMapping == null)
//                    this.m_PropertyRelationshipMapping = Condition.GetPropertyRelationshipMapping(this.Entity, this.ChildrenProperty);
//                return this.m_PropertyRelationshipMapping;
//            }
//        }

//        protected object ValueForMatch()
//        {
//            object valueToUse = this.Value;
//            if (this.PropertyRelationshipMapping != null)
//                valueToUse = Condition.ValueForMatch(this.PropertyRelationshipMapping.TargetEntityDefinition.Properties[this.Property].Field, this.Operator, valueToUse);
//            return valueToUse;
//        }

//        protected object ValueForSelect()
//        {
//            object valueToUse = this.Value;
//            if (this.PropertyRelationshipMapping != null)
//                valueToUse = this.Operator == "NOT IN" || this.Operator == "IN" ? (object)Condition.ValueToPredicateValue(this.PropertyRelationshipMapping.TargetEntityDefinition.Properties[this.Property].Field, valueToUse.ToString()) : Condition.ValueToOperator(this.PropertyRelationshipMapping.TargetEntityDefinition.Properties[this.Property].Field, valueToUse);
//            return valueToUse;
//        }

//        public override bool PerformNode()
//        {
//            this.TracePerformNode();
//            return this.CheckCondition();
//        }

//        public virtual bool CheckCondition()
//        {
//            IEntityCollection dataSourceChildren = this.GetDataSourceChildren(this.EntityName, this.ChildrenProperty);
//            if (dataSourceChildren == null)
//                return false;
//            if (string.IsNullOrEmpty(this.Property))
//            {
//                this.AddErrorMessage("ErrorPropertyIsNull");
//                return false;
//            }
//            bool flag = false;
//            object obj = this.ValueForMatch();
//            foreach (IEntity entity in (IEnumerable)dataSourceChildren)
//            {
//                flag = Condition.Match(entity.Get(this.Property), this.Operator, obj);
//                if (!flag)
//                    break;
//            }
//            if (flag)
//                this.TraceDebug("TraceChildAllMatchConditionNodeMatch");
//            else
//                this.TraceDebug("TraceChildAllMatchConditionNodeUnmatch");
//            return flag;
//        }

//        public override string AutoName()
//        {
//            return string.Format(this.GetMessage("NodeChildAllMatchConditionNameFormat"), (object)this.ChildrenProperty, (object)this.Property, (object)this.Operator, this.Value);
//        }

//        public override IList<IEntity> GetUsedEntities()
//        {
//            if (this.Value is IEntity)
//                this.AddUsedEntity((IEntity)this.Value);
//            return this.UsedEntities;
//        }

//    }
//}
