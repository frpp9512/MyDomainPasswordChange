<h1>Servicio de cambio de contraseña de dominio</h1>
<h6>Aplicación web para la actualización de la contraseña de los usuarios de un dominio basado en Directorio Activo</h6>

<article>
    <h3>¿Porque este servicio?</h3>
    <p>
        El Servicio de Cambio de Contraseñas del dominio surge precisamente para lograr que todos los usuarios (los que acceden desde la red empresarial y los que acceden a travez de la VPN) puedan ser notificados oportunamente de tiempo de vencimiento de su contraseña y cuenten con una vía de acceso para cambiarla.
    </p>
</article>

<div class="container">
                <h3>Características del servicio</h3>
                <div class="row">
                    <div class="col-12 col-lg-4 col-sm-4 text-center p-2">
                        <div>
                            <h4>Acceso web cifrado</h4>
                            <p>
                                La plataforma es accesible via web utilizando acceso cifrado via HTTPS. Puede ser abierto desde cualquier navegador actual (Chrome, Edge, Firefox, etc.)
                            </p>
                        </div>
                    </div>
                    <div class="col-12 col-lg-4 col-sm-4 text-center p-2">
                        <div>
                            <h4>Notificaciones vía correo electrónico</h4>
                            <p>
                                Recibirá todas las notificaciones referentes al estado de vencimiento de su contraseña, así como alertas de intentos fallidos y cambios realizados a tu clave de acceso.
                            </p>
                        </div>
                    </div>
                    <div class="col-12 col-lg-4 col-sm-4 text-center p-2">
                        <div>
                            <h4>Seguridad</h4>
                            <p>
                                Implementa varias capas de seguridad para garantizar al máximo la protección de sus credenciales.
                            </p>
                        </div>
                    </div>
                </div>
            </div>
            
<div>
  <h3>Detalles técnicos</h3>
  <article>
    <h4>Desarrollo</h4>
    <p>
      El sistema fue desarrollado en la tecnología <a href="https://dotnet.microsoft.com/apps/aspnet">ASP.NET</a> en la plataforma <a href="https://dotnet.microsoft.com/">.NET</a>, en su <a href="https://devblogs.microsoft.com/dotnet/announcing-net-5-0/">Versión 5</a>. Fue usado como IDE de desarrollo <a href="https://visualstudio.microsoft.com/vs/community/">Visual Studio 2019 Community</a>. El lenguaje de programación utilizado para la lógica fue <a href="https://docs.microsoft.com/en-us/dotnet/csharp/">C#</a> en su <a href="https://docs.microsoft.com/en-us/dotnet/csharp/whats-new/csharp-9">Versión 9.0</a>.
    </p>
    <h4>Estructura de la solución</h4>
    <p>
      La solución cuenta con 3 proyectos:
      <ul>
          <li>Aplicación web: <span><strong>MyDomainPasswordChange</strong></span></li>
          <li>Librería de gestión: <span><strong>MyDomainPasswordChange.Management</strong></span></li>
          <li>Servicio para el chequeo y notificación: <span><strong>PasswordExpirationCheckService</strong></span></li>
      </ul>
    </p>
  </article>
