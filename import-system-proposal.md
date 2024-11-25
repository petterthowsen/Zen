1. Namespace map to directories. Namespaces can nest, just like directories.
2. A Package maps a root directory to a root namespace. Packages contain namespaces and modules. Top-level namespaces are packages.
3. A Module is a .zen file that defines 1 or more symbols (classes, functions, possibly more)

# Syntax
- 'import namespace' imports all symbols directly under the given namespace and brings them into the scope.
- 'import package/namespace1/module/symbol' would import a function or class called 'symbol' from the given namespace.
- 'import package/namespace/module' imports all symbols from the given module.
- 'from package/namespace/module import helloFunc'  imports the 'helloFunc' symbol from the given module.
- 'from package/namespace import module' imports the helloFunc as well as any other symbols from the given module.
- 'from package/namespace/module import helloFunc, helloClass' imports multiple symbols from a module. 

In some cases, an import statement can point to a directory, or a file. If it points to a directory, the importer will assume the symbol being requested is a file. The importer must handle both cases.

# Execution
When the zen runtime executes a .zen file, it should consider it a "main script".

When the main script encounters an import statement, we should import the symbols by first seeing if the symbol can be found in the given package/namespace. If it's found, we then execute the module (the .zen file) that defines the symbol. Importantly, modules should be executed in their own scope - and, they can also import other symbols into their scope. When a module is executed due to being imported, the resulting module's own environment will be cached for further use. So subsequent imports to the same module doesn't execute again.

Also, we need to make sure cyclic imports are supported.

# Module Sources
A script should be able to import modules from three main package sources:
1. Embedded "built in" packages. These are stored in the Zen runtime and included as embedded resources. They're stored in `execution/builtins` directory - each subdirectory there is considered a built in package.
2. From installed third-party packages stored somewhere on the system. The path of which should probably be an environment key like ZEN_HOME. Each folder in this directory would be scanned for packages, and each must contain a package.zen file that names that package.
3. From modules defined by the currently running 'main script', more on this below.

## Main script and Implicit default namespace
The currently executing package (a 'main script' should be thought of as running in an implicit 'default' package whose root directory is the directory of the executing main script. If a 'package.zen' file exists in this directory, the name would be the name denoted in that file)