﻿using System;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Irony.Parsing;
using System.Collections.Generic;

namespace DynamicGeometry
{
    public class ExpressionTreeBuilder
    {
        public ExpressionTreeBuilder()
        {
            Binder = new Binder();
        }

        public Binder Binder { get; set; }
        CompileResult Status { get; set; }

        public Expression<Func<double, double>> CreateFunction(ParseTreeNode root, CompileResult status)
        {
            Status = status;
            ParameterExpression parameter = Expression.Parameter(typeof(double), "x");
            Binder.RegisterParameter(parameter);
            Expression body = CreateExpressionCore(root);
            if (body == null)
            {
                return null;
            }
            var expressionTree = Expression.Lambda<Func<double, double>>(body, parameter);
            return expressionTree;
        }

        public Expression<Func<double>> CreateExpression(ParseTreeNode root, CompileResult status)
        {
            Status = status;
            Expression body = CreateExpressionCore(root);
            if (body == null)
            {
                return null;
            }
            var expressionTree = Expression.Lambda<Func<double>>(body);
            return expressionTree;
        }

        Expression CreateExpressionCore(ParseTreeNode root)
        {
            switch (root.Term.Name)
            {
                case "UnExpr":
                    return CreateUnaryExpression(root);
                case "BinExpr":
                    return CreateBinaryExpression(root);
                case "identifier":
                    return CreateIdentifierExpression(root);
                case "number":
                    return CreateLiteralExpression(Convert.ToDouble(root.Token.Value));
                case "FunctionCall":
                    return CreateCallExpression(root);
                case "PropertyAccess":
                    return CreatePropertyAccessExpression(root);
                default:
                    return null;
            }
        }

        Expression CreateUnaryExpression(ParseTreeNode root)
        {
            var unaryOperator = root.ChildNodes[0];
            Expression operand = CreateExpressionCore(root.ChildNodes[1]);
            
            if (operand == null)
            {
                return null;
            }

            if (unaryOperator.Term.Name == "-")
            {
                return Expression.Negate(operand);
            }

            return null;
        }

        Expression CreateIdentifierExpression(ParseTreeNode root)
        {
            var text = root.Token.Text;
            Expression resolveTwoPoints = ResolveTwoPoints(text);
            if (resolveTwoPoints != null)
            {
                return resolveTwoPoints;
            }
            var parameter = Binder.Resolve(text);
            if (parameter == null)
            {
                Status.AddUnknownIdentifierError(text);
            }
            return parameter;
        }

        public Expression ResolveTwoPoints(string twoPoints)
        {
            var drawing = Binder.Drawing;
            if (drawing == null)
            {
                return null;
            }

            var names = drawing.Figures.Where(f => f is PointBase).Select(f => f.Name).ToArray();
            string longestPrefix = "";
            string longestSuffix = "";
            foreach (var name in names)
            {
                if (twoPoints.StartsWith(name, StringComparison.OrdinalIgnoreCase) && name.Length > longestPrefix.Length)
                {
                    longestPrefix = name;
                }
                if (twoPoints.EndsWith(name, StringComparison.OrdinalIgnoreCase) && name.Length > longestSuffix.Length)
                {
                    longestSuffix = name;
                }
            }

            if (longestPrefix.Length + longestSuffix.Length == twoPoints.Length)
            {
                PointBase point1 = drawing.Figures[longestPrefix] as PointBase;
                PointBase point2 = drawing.Figures[longestSuffix] as PointBase;

                if (point1 == null)
                {
                    Status.AddFigureIsNotAPointError(longestPrefix);
                    return null;
                }
                if (point2 == null)
                {
                    Status.AddFigureIsNotAPointError(longestSuffix);
                    return null;
                }
                if (!Binder.FigureAllowed(point1))
                {
                    Status.AddDependencyCycleError(longestPrefix);
                    return null;
                }
                if (!Binder.FigureAllowed(point2))
                {
                    Status.AddDependencyCycleError(longestSuffix);
                    return null;
                }

                ConstantExpression p1 = Expression.Constant(point1);
                ConstantExpression p2 = Expression.Constant(point2);
                MethodInfo distance = typeof(Math).GetMethod("Distance",
                    new[] { typeof(PointBase), typeof(PointBase) });
                MethodCallExpression result = Expression.Call(null, distance, p1, p2);
                Status.Dependencies.Add(point1);
                Status.Dependencies.Add(point2);
                return result;
            }

            return null;
        }

        Expression CreatePropertyAccessExpression(ParseTreeNode root)
        {
            string figureName = root.ChildNodes[0].Token.Text;
            string propertyName = root.ChildNodes[1].Token.Text;
            IFigure figure = Binder.ResolveFigure(figureName);
            if (figure == null)
            {
                Status.AddUnknownIdentifierError(figureName);
                return null;
            }
            if (!Binder.IsFigureAllowed(figure))
            {
                Status.AddDependencyCycleError(figureName);
                return null;
            }
            Type type = figure.GetType();
            var property = type.GetProperty(propertyName,
                BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance);
            if (property == null)
            {
                Status.AddPropertyNotFoundError(figure, propertyName);
                return null;
            }
            Status.Dependencies.Add(figure);
            var figureExpression = Expression.Constant(figure);
            var propertyExpression = Expression.Property(figureExpression, property);
            return propertyExpression;
        }

        Expression CreateCallExpression(ParseTreeNode root)
        {
            string functionName = root.ChildNodes[0].Token.Text;
            MethodInfo method = Binder.ResolveMethod(functionName);
            if (method == null)
            {
                Status.AddMethodNotFoundError(functionName);
                return null;
            }

            var arguments = root.ChildNodes[1];
            if (arguments.Term != null && arguments.Term.Name == "ArgumentList")
            {
                return CreatePointFunctionCallExpression(method, arguments);
            }

            Expression argument = CreateExpressionCore(root.ChildNodes[1]);
            if (argument == null)
            {
                return null;
            }
            return Expression.Call(method, argument);
        }

        Expression CreatePointFunctionCallExpression(MethodInfo method, ParseTreeNode arguments)
        {
            List<IPoint> points = new List<IPoint>();
            foreach (var node in arguments.ChildNodes)
            {
                string pointName = node.Token.Text;
                var point = ResolvePoint(pointName);
                if (point == null)
                {
                    return null;
                }
                points.Add(point);
            }

            if (method.Name == "Area")
            {
                return Expression.Call(method, Expression.Constant(points.ToArray()));
            }

            if (method.GetParameters().Length != points.Count)
            {
                Status.AddIncorrectNumberOfArgumentsError(method, arguments.ChildNodes.Count);
                return null;
            }
            
            List<Expression> pointArguments = new List<Expression>();
            foreach (var point in points)
            {
                Expression pointArgument = CreatePointExpression(point);
                if (pointArgument == null)
                {
                    return null;
                }
                pointArguments.Add(pointArgument);
            }

            return Expression.Call(method, pointArguments.ToArray());
        }

        Expression CreatePointExpression(IPoint point)
        {
            var pointExpression = Expression.Constant(point);
            var coordinatesProperty = typeof(IPoint).GetProperty("Coordinates");
            var coordinates = Expression.Property(pointExpression, coordinatesProperty); 
            return coordinates;
        }

        IPoint ResolvePoint(string pointName)
        {
            IFigure figure = Binder.ResolveFigure(pointName);
            if (figure == null)
            {
                Status.AddUnknownIdentifierError(pointName);
                return null;
            }
            IPoint point = figure as IPoint;
            if (point == null)
            {
                Status.AddFigureIsNotAPointError(pointName);
                return null;
            }
            Status.Dependencies.Add(point);
            return point;
        }

        Expression CreateLiteralExpression(double arg)
        {
            return Expression.Constant(arg);
        }

        Expression CreateBinaryExpression(ParseTreeNode node)
        {
            Expression left = CreateExpressionCore(node.ChildNodes[0]);
            Expression right = CreateExpressionCore(node.ChildNodes[2]);

            if (left == null || right == null)
            {
                return null;
            }

            switch (node.ChildNodes[1].Term.Name)
            {
                case "+":
                    return Expression.Add(left, right);
                case "-":
                    return Expression.Subtract(left, right);
                case "*":
                    return Expression.Multiply(left, right);
                case "/":
                    return Expression.Divide(left, right);
                case "^":
                    return Expression.Power(left, right);
            }
            return null;
        }

        public void SetContext(Drawing drawing, Predicate<IFigure> isFigureAllowed)
        {
            Binder.Drawing = drawing;
            Binder.FigureAllowed = isFigureAllowed;
        }
    }
}
