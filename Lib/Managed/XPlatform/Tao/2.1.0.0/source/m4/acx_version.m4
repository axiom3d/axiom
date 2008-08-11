AC_DEFUN([ACX_ASSEMBLY_VERSION], [
  AC_MSG_CHECKING([for $1 assembly version])
  $3="`sed -ne 's/.*AssemblyVersion("\(.*\)").*/\1/p' src/$1/Properties/AssemblyInfo.cs`"
  AC_SUBST($3)
  $2="`echo "$$3" | sed 's/@<:@.@:>@@<:@0-9@:>@*@<:@.@:>@@<:@0-9@:>@*$//'`"
  AC_SUBST($2)
  AC_MSG_RESULT([$$2 ($$3)])
])
