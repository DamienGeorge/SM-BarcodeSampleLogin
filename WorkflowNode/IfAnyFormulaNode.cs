using System.Collections;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using System.Xml.Linq;
using Thermo.SampleManager.Common;
using Thermo.SampleManager.Common.Data;
using Thermo.SampleManager.Library;
using Thermo.SampleManager.Server.Workflow.Attributes;
using Thermo.SampleManager.Server.Workflow.Definition;
using Thermo.SampleManager.Server.Workflow.Helpers;

namespace Thermo.SampleManager.Server.Workflow.Nodes
{
    /// <summary>
    /// Bag View Node
    /// </summary>
    [WorkflowNode(NodeType, "NodeIfAnyFormulaName", "NodeIfAnyFormulaCategory", "CHECKS", "NodeIfAnyFormulaDescription", "WorkflowMessages")]
    [Tag("CONDITION")]
    [FollowsTag("DATA")]
    [FollowsTag("CONDITION_SET")]
    public class IfAnyFormulaNode : ChildAllMatchConditionNode
    {
        #region Constants

        /// <summary>
        /// The workflow node type this node implements
        /// </summary>
        public const string NodeType = "IF_ANY_FORMULA";

        #endregion

        #region Constructor

        /// <summary>
        /// Initializes a new instance of the <see cref="MessageBoxNode"/> class.
        /// </summary>
        /// <param name="node"></param>
        public IfAnyFormulaNode(WorkflowNodeInternal node) : base(node)
        {
        }

        #endregion

        #region Parameters
        //[DataSourceParameter("ENTITY_NAME", "NodeIfAnyFormulaParameterEntity", "NodeIfAnyFormulaParameterEntityDescription")]
        //public string EntityName
        //{
        //    get => this.GetParameterBagValue<string>("ENTITY_NAME");
        //    set => this.SetParameterBagValue("ENTITY_NAME", (object)value);
        //}

        //[LinkedEntityTypePropertyParameter("CHILDREN_PROPERTY", "NodeIfAnyFormulaParameterChildren", "NodeIfAnyFormulaParameterChildrenDescription", "ENTITY_NAME", PropertyFilter = PropertyFilter.Collection)]
        //public string ChildrenProperty
        //{
        //    get => this.GetParameterBagValue<string>("CHILDREN_PROPERTY");
        //    set => this.SetParameterBagValue("CHILDREN_PROPERTY", (object)value);
        //}

        //[ChildEntityTypePropertyParameter("PROPERTY", "NodeIfAnyFormulaParameterProperty", "NodeIfAnyFormulaParameterPropertyDescription", "ENTITY_NAME", "CHILDREN_PROPERTY", PropertyFilter = PropertyFilter.NonCollection)]
        //public string Property
        //{
        //    get => this.GetParameterBagValue<string>("PROPERTY");
        //    set => this.SetParameterBagValue("PROPERTY", (object)value);
        //}

        //[ChildPropertyConditionOperatorParameter("OPERATOR", "NodeIfAnyFormulaParameterOperator", "NodeIfAnyFormulaParameterOperatorDescription", "ENTITY_NAME", "PROPERTY", "CHILDREN_PROPERTY")]
        //public string Operator
        //{
        //    get => this.GetParameterBagValue<string>("OPERATOR", "=");
        //    set => this.SetParameterBagValue("OPERATOR", (object)value);
        //}

        [ChildPropertyConditionValueParameter("VALUE", "NodeIfAnyFormulaParameterValue", "NodeIfAnyFormulaParameterValueDescription", "ENTITY_NAME", "PROPERTY", "OPERATOR", "CHILDREN_PROPERTY")]
        public string Value
        {
            get => this.GetParameterBagValue<string>("VALUE");
            set => this.SetParameterBagValue("VALUE", value);
        }

        #endregion

        #region Perform

        public override bool CheckCondition()
        {
            var formulaText = GetFormulaText(Value);
            object valueToUse = this.ValueForSelect();
            try
            {
                if (this.Entity.State == EntityState.Unchanged)
                {
                    if (this.PropertyRelationshipMapping != null)
                    {
                        bool match;
                        if (this.CheckConditionUsingQuery(this.Entity, this.PropertyRelationshipMapping, valueToUse, out match))
                        {
                            if (match)
                                this.TraceDebug("TraceChildAnyMatchConditionNodeMatch");
                            else
                                this.TraceDebug("TraceChildAnyMatchConditionNodeUnmatch");
                            return match;
                        }
                    }
                }
            }
            catch
            {
            }
            IEntityCollection dataSourceChildren = this.GetDataSourceChildren(this.EntityName, this.ChildrenProperty);
            if (dataSourceChildren == null)
                return false;
            if (string.IsNullOrEmpty(this.Property))
            {
                this.AddErrorMessage("ErrorPropertyIsNull");
                return false;
            }
            object obj = this.ValueForMatch();
            foreach (IEntity entity in (IEnumerable)dataSourceChildren)
            {
                if (Condition.Match(entity.Get(this.Property), this.Operator, obj))
                {
                    this.TraceDebug("TraceChildAnyMatchConditionNodeMatch");
                    return true;
                }
            }
            this.TraceDebug("TraceChildAnyMatchConditionNodeUnmatch");
            return false;
        }

        private bool CheckConditionUsingQuery(
          IEntity entity,
          TableRelationshipMapping propertyRelationshipMapping,
          object valueToUse,
          out bool match)
        {
            match = false;
            SQLQuery sqlQuery = (SQLQuery)null;
            foreach (ISchemaRelationshipPredicate predicate in (IEnumerable)propertyRelationshipMapping.Relationship.Predicates)
            {
                if (sqlQuery == null)
                {
                    sqlQuery = new SQLQuery(entity.EntityManager);
                    sqlQuery.AddSQL(string.Format("select count([{0}]) from [{1}] where [{0}] = '{2}'", (object)predicate.DestinationField.Name, (object)propertyRelationshipMapping.TargetEntityDefinition.DataSource, (object)entity.GetString(predicate.SourceField.Name)));
                }
                else
                    sqlQuery.AddSQL(string.Format(" and [{0}] = '{1}'", (object)predicate.DestinationField.Name, (object)entity.GetString(predicate.SourceField.Name)));
            }
            if (sqlQuery == null)
                return false;
            if (this.Operator == "NOT IN" || this.Operator == "IN")
            {
                sqlQuery.AddSQL(string.Format(" and [{0}] {1} ({2})", (object)propertyRelationshipMapping.TargetEntityDefinition.Properties[this.Property].Field.Name, (object)this.Operator, valueToUse));
            }
            else
            {
                switch (valueToUse)
                {
                    case string _:
                        sqlQuery.AddSQL(string.Format(" and [{0}] {1} '{2}'", (object)propertyRelationshipMapping.TargetEntityDefinition.Properties[this.Property].Field.Name, (object)this.Operator, valueToUse));
                        break;
                    case IEntity _:
                        if ((propertyRelationshipMapping.TargetEntityDefinition.Properties[this.Property] is TableRelationshipMapping property ? property.Relationship?.Predicates : (IList)null) == null || property.Relationship.Predicates.Count == 0)
                            return false;
                        IEnumerator enumerator = property.Relationship.Predicates.GetEnumerator();
                        try
                        {
                            while (enumerator.MoveNext())
                            {
                                if (!(enumerator.Current is ISchemaRelationshipPredicate current))
                                    return false;
                                sqlQuery.AddSQL(string.Format(" and [{0}] {1} '{2}'", (object)current.SourceField.Name, (object)this.Operator, (object)((IEntity)valueToUse).GetString(current.DestinationField.Name)));
                            }
                            break;
                        }
                        finally
                        {
                            if (enumerator is IDisposable disposable)
                                disposable.Dispose();
                        }
                    default:
                        return false;
                }
            }
            sqlQuery.Columns.AddInteger();
            IDataReader dataReader = (IDataReader)null;
            try
            {
                dataReader = sqlQuery.ExecuteReader();
                if (dataReader.Read())
                {
                    match = (int)dataReader[0] != 0;
                    return true;
                }
            }
            finally
            {
                dataReader?.Close();
            }
            return false;
        }
        #endregion

        public override string AutoName()
        {
            return string.Format(this.GetMessage("NodeChildAnyMatchConditionNameFormat"), (object)this.ChildrenProperty, (object)this.Property, (object)this.Operator, this.Value);
        }

    }
}
