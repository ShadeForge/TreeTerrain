using UnityEngine;
using UnityEditor;
using System.Collections;
using System;

/// <summary>
/// Der SimpleMeshDebugger
/// 
/// Bei der Generierung von Meshes benötigt man immer wieder ein visuelles 
/// Feedback um sich zu vergewissern, dass alle Vertex-Positionen stimmen, 
/// Triangles richtig aufgespannt sind oder UV-Koordinaten richtig berech-
/// net sind.
/// 
/// Hierbei soll der SimpleMeshDebugger helfen.
/// 
/// Er ist eine Sammlung von Darstellungsmethoden, die die folgenden Mesh-
/// Informationen im Szenenfenster direkt am Mesh darstellen:
/// 
///     - Vertex Positionen (als Vector3)
///     - Vertex Indices 
///             (also die Reihenfolge der Vertices im Array) (als Integer)
///     - Vertex Normals 
///             (als Linie in Richtung der Normalen)
///     - UV-Koordinaten 
///             für jeden Vertex (als Vector2)
///     - Triangle Normals 
///             (als Linie in Richtung der Normalen)
///     - Triangle Numbers 
///             (also eine laufende Nummer für die Triangles) (als Integer)
///     - Triangle Vertex Indices 
///             (die vom Dreieck referenzierten Vertex-Indices) (als Integer)
///     - Triangle UVs 
///             (also die UV-Koordinaten der vom Dreieck referenzierten 
///             Vertices) (als Vector2)
/// 
/// Zusätzlich soll der SimpleMeshDebugger dazu dienen die Unity-Mesh-Daten-
/// struktur kennenzulernen und untersuchen zu können.
/// 
/// Obwohl es eine Vielzahl an Optimierungsmöglichkeiten gibt, die den Simple-
/// MeshDebugger auch für größere Meshes performant benutzbar zu machen, wurde
/// hier gezielt darauf verzichtet, da hierdurch der die Übersichtlichkeit der
/// Mesh-Informationen (zum Kennenlernen des Meshes) verloren gehen würde.
/// </summary>
[CustomEditor(typeof(SimpleMeshDebugger))]
public class SimpleMeshDebuggerEditor : Editor
{

    // Referenz auf die SimpleMeshDebugger (MonoBehaviour) Instanz
    private SimpleMeshDebugger debugger;

    #region Komponenten des Objekts
    // Die Transform-Komponente des 3D-Objekts
    private Transform t;
    // Der MeshFilter des GameObjects
    private MeshFilter meshFilter;
    // Der MeshRenderer des GameObjects
    private MeshRenderer meshRenderer;
    // Das Mesh des 3D-Objekts
    private Mesh mesh;
    #endregion // Komponenten des Objekts


    /// <summary>
    /// Culling: 
    /// Als Culling wird das Verwerfen/Ignorieren von nicht-sichtbaren Objekten
    /// oder Objekt-Teilen in der 3D-Grafik bezeichnet.
    /// 
    /// Wir verwenden ein primitives Verfahren um die Mesh-Informationen nur
    /// für wirklich sichtbare Vertices und Triangles darzustellen.
    /// 
    /// Auf die Vorgehensweise des Verfahrens, wird weiter unten eingegangen.
    /// 
    /// P.S.: An dieser Stelle ist das größte Optimierungspotential bei Verwen-
    ///       dung komplizierterer Verfahren.
    /// </summary>
    #region Culling
    // Der Collider des GameObjects
    private Collider collider;
    // Array zum Speichern der Sichtbarkeit von Vertices
    private bool[] vertexVisible;
    // Die Kamera des Szenenfensters in dem das Objekt gezeichnet wird
    private Camera camera;
    #endregion // Culling


    /// <summary>
    /// Der Mesh-Debugger soll eine simple Editierbarkeit des Meshes bereit-
    /// stellen. Hierzu werden Handles für alle Vertices erzeugt und deren
    /// Benutzung behandelt.
    /// 
    /// Der Edit-Mode soll das Mesh in den Ausgangs-Zustand zurück versetzen, 
    /// wenn das Game-Objekt in der Szene deselektiert wird.
    /// </summary>
    #region Vertex-Editor
    private bool vertexEditMode;
    public Vector3[] originalVertices;
    #endregion // VertexEditor


    /// <summary>
    /// Toggle Infos
    /// Wir halten uns bool'sche Variablen um die einzelnen Mesh-Informationen
    /// im Editor an/aus-schaltbar zu machen
    /// </summary>
    #region Toggle Infos
    // Vertex Positionen
    public static bool showVertexPosition;
    // Vertex Indices (die Reihenfolge der Vertices im Array) 
    public static bool showVertexIndices;
    // Vertex Normals 
    public static bool showVertexNormals;
    // UV-Koordinaten
    public static bool showUVs;
    // Triangle Normals
    public static bool showTriangleNormals;
    // Triangle Numbers 
    public static bool showTriangleNumbers;
    // Triangle Vertex Indices
    public static bool showTriangleVertexIndices;
    // Triangle UVs 
    public static bool showTriangleUVs;
    #endregion // Toggle Infos


    /// <summary>
    /// Der SimpleMeshDebuggerEditor "erwacht" sobald ein Objekt in der Szene
    /// ausgewählt wird, dass die Komponente "SimpleMeshDebugger" besitzt.
    /// </summary>
    void Awake()
    {
        // Wir holen uns die SimpleMeshDebugger-Komponente des Objekts
        // Das "target"-Objekt wird von Unity bereitgestellt und ist in
        // diesem Fall die SimpleMeshDebugger-Komponente 
        debugger = target as SimpleMeshDebugger;

        // Wir holen uns die Transform-Komponente des Objekts
        // (diese ist auf JEDEN Fall vorhanden)
        t = this.debugger.transform;

        // Wir holen uns den MeshFilter des Objekts...
        this.meshFilter = this.debugger.GetComponent<MeshFilter>();
        // ... oder fügen einen hinzu, sollte keiner vorhanden sein
        if (meshFilter == null)
            this.meshFilter = this.debugger.gameObject.AddComponent<MeshFilter>();

        // Wir holen uns den MeshRenderer des Objekts...
        this.meshRenderer = this.debugger.GetComponent<MeshRenderer>();
        // ... oder fügen einen hinzu, sollte keiner vorhanden sein
        if (meshRenderer == null)
            this.meshRenderer = this.debugger.gameObject.AddComponent<MeshRenderer>();

        // Wir holen uns den Collider des Objekts...
        this.collider = this.debugger.GetComponent<Collider>();
        // ... oder fügen einen hinzu, sollte keiner vorhanden sein
        if (this.collider == null)
            this.collider = this.debugger.gameObject.AddComponent<MeshCollider>();

        // Wir holen uns das sharedMesh vom MeshFilter des Objekts
        // (Bei Verwendung im Editor muss immer das sharedMesh und nicht das mesh
        //  geholt werden, da sonst möglicherweise immer neue Instanzen angelegt
        //  werden könnten. Unity beschwert sich darüber...)
        this.mesh = meshFilter.sharedMesh;

        // Wir speichern die ursprünglichen Positionen der Vertices in einem
        // "Backup" Array um das Mesh am Ende wieder in den Ursprungszustand 
        // versetzen zu können.
        this.originalVertices = this.mesh.vertices;
    }


    /// <summary>
    /// Behandlung von Eingaben und Darstellung im Szenenfenster
    /// </summary>
    void OnSceneGUI()
    {
        // Ist kein Mesh vorhanden, müssen wir nichts machen und beenden hier
        // frühzeit die Methode
        if (mesh == null)
            return;

        // Vorbereiten fürs Culling --> Welche Vertices sind sichtbar?
        PrepareCulling();

        // Vertex-Positionen darstellen, wenn gewünscht...
        if (showVertexPosition)
            ShowVertexPosition();

        // Vertex-Indices darstellen, wenn gewünscht...
        if (showVertexIndices)
            ShowVertexIndices();

        // Vertex-Normals darstellen, wenn gewünscht...
        if (showVertexNormals)
            ShowVertexNormals();

        // UV-Koordinaten darstellen, wenn gewünscht...
        if (showUVs)
            ShowUVs();

        // Triangle-Normalen darstellen, wenn gewünscht...
        if (showTriangleNormals)
            ShowTriangleNormals();

        // Triangle-Nummerierung darstellen, wenn gewünscht...
        if (showTriangleNumbers)
            ShowTriangleNumbers();

        // Vertex-Indices der Triangles darstellen, wenn gewünscht...
        if (showTriangleVertexIndices)
            ShowTriangleVertexIndices();

        // UV-Koordinaten auf dem Triangle darstellen, wenn gewünscht...
        if (showTriangleUVs)
            ShowTriangleUVs();
    }

    /// <summary>
    /// Für das Culling werden zuerst alle Vertices auf Sichtbarkeit geprüft.
    /// Hierzu 
    /// 
    /// </summary>
    private void PrepareCulling()
    {
        // Wir deklarieren ein bool-Array in dem wir für jeden Vertex
        // speichern können, ob der Vertex gerade sichtbar ist oder nicht.
        vertexVisible = new bool[mesh.vertexCount];

        // Sicherheitshalber überprüfen wir, ob ein Collider vorhanden ist.
        if (collider != null)
        {
            if (collider is MeshCollider)
            {
                MeshCollider meshCollider = (MeshCollider) collider;
                meshCollider.sharedMesh = mesh;
            }
            // Dann laufen wir alle Vertices durch...
            for (int i = 0; i < this.mesh.vertexCount; i++)
            {
                // ... und überprüfen ob die Position des Vertex sichtbar ist.
                vertexVisible[i] = PositionVisibleObjectRelated(
                    t.TransformPoint(this.mesh.vertices[i]), collider);
            }
        }
    }

    /// <summary>
    /// Prüfe für eine Position ob sie von der Karma aus sichtbar ist.
    /// 
    /// Dies geschieht über den Collider des Objekts. Der Collider ist normal-
    /// erweise dazu da um bei GameObjekten auf Kollision in der physikalisch-
    /// en Simulation zu prüfen.
    /// Wir können Ihn hier "missbrauchen" und "schießen" einen Strahl (Ray)
    /// von der Kamera aus in Richtung der zu prüfenden Position. Trifft der 
    /// Strahl auf ein Objekt, können wir überprüfen ob der "Trefferpunkt" nah
    /// genug an unserer Position liegt.
    /// 
    /// Liegt der punkt nah genug (kleiner 0.02) an unserer zu prüfenden
    /// Position, gehen wir davon aus, dass die Position sichtbar ist.
    /// Ist dies nicht der Fall, so ist der Strahl vorher auf ein Objekt 
    /// getroffen, welches die Sichtbarkeit versperrt.
    /// 
    /// Wir schauen dabei nur 100 Einheit in Richtung der Position. Ist sie 
    /// weg, zählen wir sie als nicht sichtbar.
    /// </summary>
    private bool PositionVisibleObjectRelated(Vector3 position, Collider collider)
    {
        // Wir holen uns die Kamera die im aktuellen Szenenfenster
        // gerade die Darstellung rendert. Wir benötigen die Kamera,
        // da wir von der Position der Kamera aus einen Strahl in 
        // Richtung der zu überprüfenden Positionen schießen wollen.
        camera = SceneView.currentDrawingSceneView.camera;

        // Wir erzeugen einen neuen Strahl, der von der Kameraposition in
        // Richtung der zu Prüfenden Position verläuft.
        Ray ray = new Ray(camera.transform.position, position - camera.transform.position);

        // Wir deklarieren einen RaycastHit
        // Die Klasse dient dazu Informationen über den Verlauf des Strahls zu 
        // speichern, wie z.B. die Stellen an denen der Strahl auf ein Objekt 
        // getroffen hat. Diese Informationen müssen wir uns allerdings im 
        // nächsten Schritt erst holen...
        RaycastHit hit;

        // Der Collider unseres Objekts bietet uns die Möglichkeit den Strahl
        // auf ihn abzufeuern und auf Kollision zu testen. Man nennt dies
        // "Raycast".
        //
        // Dazu übergeben wir unseren Ray und den RaycastHit hit in dem wir 
        // Treffer speichern wollen (durch die Verwendung des "out"-Parameters
        // wird die Variable so übergeben, dass darin geschrieben werden kann).
        //
        // Als letzen Parameter legen wir fest, dass nur 100 Längen-Einheiten  
        // des Strahls berücksichtigt werden sollen.
        //
        // Bei der Ausführung von collider.Raycast wird true zurückgeliefert, 
        // sollte der Strahl auf etwas treffen. Andernfalls false.
        bool hitSomething = collider.Raycast(ray, out hit, 100f);
        
        // Ist der Strahl auf etwas getroffen, müssen wir entscheiden, ob
        // die Position plausibel sichtbar ist oder nicht.
        if (hitSomething)  
        {
            // Ist der Abstand des Treffers näh genug an der zu prüfenden
            // Position, gehen wir davon aus, dass die Position sichtbar ist.
            // Eine leichte Differenz der Positionen kann vorkommen, da der
            // Collider den wir zur Überprüfung verwenden nicht zwingend exakt
            // unserer Mesh-Geometrie entspricht.
            // Z.B. ein SphereCollider (der für Kugeln verwendet wird) macht
            // die Kollisionsprüfung rechnerisch über den Radius der Kugel und
            // nicht wie ein MeshCollider über die Mesh-Geometrie.
            // Da die Kugel durch die Triangles nur "angenähert" wird, kommt
            // es hier möglicherweise zu einer abweichenden Trefferposition. 
            if (Vector3.Distance(hit.point, position) < 0.02f)
                return true;
            return false;
         }

        // Sollte kein Treffer erfolgt sein, ist einer der folgenden Fälle
        // eingetreten:
        // 1. Die Position ist sichtbar, aber der Collider wurde nicht getroffen
        // Oder
        // 2. Die Position liegt weiter weg als 100 Einheiten.
        //      Wir können nicht sagen ob die Position sichtbar ist oder nicht
        //      und gehen davon aus, dass sie sichtbar ist.
        return true;
    }

    #region Mesh-Info Darstellungs-Methoden
    /// <summary>
    /// Diese Methode soll die Vertex-Positionen ausgeben.
    /// 
    /// An der Position des Vertex soll ein Label ausgegeben werden, welches
    /// die Position des Vertex ausgibt.
    /// 
    /// Darstellung von Vector3.ToString():  [0.0, 1.75, 3.25]
    /// </summary>
    private void ShowVertexPosition()
    {
        // Alle Vertices durchgehen...
        for (int i = 0; i < this.mesh.vertexCount; i++)
        {
            // ... culling --> ist der Vertex sichtbar?
            if (vertexVisible[i])
            {
                // Zeige ein Label im Szenenfenster:
                // - an der Position des Vertex 
                //   (die Position wird abhängig von der Transform-Komponente
                //    transformiert, lokale --> globale Koordinaten)
                //    ---> Hierdurch wird sichergestellt, dass das Label
                //      bei beliebiger Position, Rotation und Skalierung des 
                //      Objekts richtig gezeichnet wird.
                // - Text ist die Position des Vertex (Vector3.ToString())
                Handles.Label(
                    t.TransformPoint(mesh.vertices[i]),
                    mesh.vertices[i].ToString());
            }
        }
    }

    /// <summary>
    /// Zeige den Index aller Vertices im Vertex-Array an
    /// 
    /// An der Position des Vertex soll ein Label ausgegeben werden, welches
    /// den Index des Vertex im Vertex-Array ausgibt.
    /// 
    /// Darstellung als Integer
    /// </summary>
    private void ShowVertexIndices()
    {
        // Alle Vertices durchgehen
        for (int i = 0; i < this.mesh.vertexCount; i++)
        {
            // ... culling --> ist der Vertex sichtbar?
            if (vertexVisible[i])
            {
                // Zeige ein Label im Szenenfenster:
                // - an der Position des Vertex 
                //   (die Position wird abhängig von der Transform-Komponente
                //    transformiert, lokale --> globale Koordinaten)
                //    ---> Hierdurch wird sichergestellt, dass das Label
                //      bei beliebiger Position, Rotation und Skalierung des 
                //      Objekts richtig gezeichnet wird.
                // - Text ist der Index im Vertex-Array als String
                Handles.Label(
                    t.TransformPoint(mesh.vertices[i]),
                    i.ToString());
            }
        }
    }

    /// <summary>
    /// Zeige die Vertex-Normalen an
    /// 
    /// An der Position des Vertex beginnt eine Linie die in Richtung 
    /// der normalen verläuft.
    /// 
    /// Die Farbe der Normalen wird aus dem Richtungsvektor abgeleitet.
    /// x,y,z --> r,g,b
    /// </summary>
    private void ShowVertexNormals()
    {
        for (int i = 0; i < mesh.vertexCount; i++)
        {
            // Die Position des Vertex wird von lokalen Koordinaten in 
            // globale Koordinaten transformiert
            Vector3 vertexPosition = t.TransformPoint(this.mesh.vertices[i]);

            // Hanle-Color setzen
            // x,y,z-Werte werden dabei als r,b,g-Werte der Farbe interpretiert
            Handles.color = new Color(
                Mathf.Abs(t.TransformDirection(mesh.normals[i]).x),
                Mathf.Abs(t.TransformDirection(mesh.normals[i]).y),
                Mathf.Abs(t.TransformDirection(mesh.normals[i]).z));

            // Die Normale wird aus dem Mesh gelesen und deren Richtungsvektor
            // bzgl. der Transform-Komponente transformiert.
            // ---> Hierdurch wird sichergestellt, dass die Richtung der normalen
            //      bei beliebiger Position, Rotation und Skalierung des Objekts
            //      richtig gezeichnet wird.
            //      Da es sich um einen Richtungsvektor handelt, verwenden wir
            //      TransformDirection und nicht TransformPoint
            Vector3 normalDirection = t.TransformDirection(this.mesh.normals[i]);

            // Zeichne eine Linie von der Vertex-Position in Richtung
            // der Normalen mit der erstellten Farbe
            Handles.DrawLine(vertexPosition, vertexPosition + normalDirection);
        }
    }

    /// <summary>
    /// Zeige den Index aller Vertices im Vertex-Array an
    /// 
    /// An der Position des Vertex soll ein Label ausgegeben werden, welches
    /// den Index des Vertex im Vertex-Array ausgibt.
    /// 
    /// Darstellung als Integer
    /// </summary>
    private void ShowUVs()
    {
        // Alle Vertices durchgehen
        for (int i = 0; i < this.mesh.vertexCount; i++)
        {
            // ... culling --> ist der Vertex sichtbar?
            if (vertexVisible[i])
            {
                // Zeige ein Label im Szenenfenster:
                // - an der Position des Vertex 
                //   (die Position wird abhängig von der Transform-Komponente
                //    transformiert, lokale --> globale Koordinaten)
                //    ---> Hierdurch wird sichergestellt, dass das Label
                //      bei beliebiger Position, Rotation und Skalierung des 
                //      Objekts richtig gezeichnet wird.
                // - Text ist die UV-Koordinate die dem Vertex zugewiesen ist
                //   Vector2.ToString()  z.B. [0.3, 0.7]
                Handles.Label(
                    t.TransformPoint(mesh.vertices[i]),
                    mesh.uv[i].ToString());
            }
        }
    }

    /// <summary>
    /// Zeige die Normalen der Triangles
    /// 
    /// Die Normalen stehen orthogonal auf der "sichtbaren" Seite des Triangles
    /// und werden aus den Punkten des Triangles berechnet.
    /// Aus den Punkten p0 und p1 wird ein Richtungsvektor berechnet, ebenso
    /// aus den Punkten p0 und p2.
    /// Diese Beiden Richtungsvektoren spannen eine Ebene auf.
    /// Mit dem Kreuzprodukt berechnen wir den Richtungsvektor der orthogonal
    /// auf dieser Ebene steht.
    /// 
    ///     Normale 
    ///         ^   p1- - - - -
    ///         |   ^        /
    ///         |  /        /
    ///         | /        /
    ///         |/        /
    ///        p0------->p2
    /// 
    /// 
    /// Wir berechnen den Centroid des Dreiecks indem wir den Summenvektor der
    /// drei Ortsvektoren unseres Dreiecks berechnen und diesen durch 3 Teilen.
    /// Für jedes beliebige Polygon (auch n-Gon mit n Punkten) können wir diesen
    /// Mittelpunkt/Schwerpunkt berechnen, indem der Summenvektor gebildet wird
    /// und durch die Anzahl der Punkte geteilt wird.
    /// 
    ///             Normale
    ///              ^
    ///              | 
    ///           p1 | 
    ///             ^|  
    ///            / |\  
    ///           /  x \ 
    ///          /      \  
    ///        p0------->p2
    /// 
    /// Wir zeichnen die Normal vom Centroid aus in Richtung der Normalen.
    /// </summary>
    private void ShowTriangleNormals()
    {
        // Laufe das Triangle-Array durch und betrachte in jedem Durchlauf
        // 3 Indices in diesem Array  -->  i += 3
        for (int i = 0; i < this.mesh.triangles.Length; i += 3)
        {
            // Die Indices der Vertices des Dreiecks bekommen wir aus dem 
            // Triangles-Array, da hier die Indices als Integer gespeichert sind
            // Den Index verwenden wir dann dazu uns die Position des Vertex
            // aus dem Vertices-Array zu holen.
            // Da wir in jedem Schleifendurchlauf i um 3 erhöhen, können wir
            // über i, i+1 und i+2 die 3 Vertices des Triangles bekommen.
            Vector3 p0 = mesh.vertices[mesh.triangles[i + 0]];
            Vector3 p1 = mesh.vertices[mesh.triangles[i + 1]];
            Vector3 p2 = mesh.vertices[mesh.triangles[i + 2]];

            // Der Centroid wird (wie oben beschrieben) berechnet...
            Vector3 centroid = (p0 + p1 + p2) / 3f;
            // ...und von lokalen auf globale Koordinaten transformiert
            centroid = t.TransformPoint(centroid);

            // Wir berechnen die Normale (wie oben beschrieben) und
            // transformieren Sie von lokalen in globale Koordinaten.
            // (Da es sich um einen Richtungsvektor handelt, verwenden wir
            //  TransformDirection und nicht TransformPoint)
            Vector3 normal = Vector3.Cross((p1 - p0), (p2 - p0));
            // Mit .normalized bekommen wir den Normalisierten 
            // Richtungsvektor (Richtung egal, Länge 1)
            normal = normal.normalized;
            // Transformiere die Normale von lokalen
            // auf globale Koordinaten
            normal = t.TransformDirection(normal);

            // Wir Interpretieren die Richtung der NOrmalen als Farbe
            // Dabei nehmen wir den Betrag der x,y,z-Komponenten,
            // da es sich auch um negative Werte handeln könnte,
            // die andernfalls als 0 interpretiert würden.
            Handles.color = new Color(
                Mathf.Abs(normal.x),
                Mathf.Abs(normal.y),
                Mathf.Abs(normal.z));

            // Wir zeichnen die Normale vom Centroid aus in Richtung der Normalen
            Handles.DrawLine(centroid, centroid + normal);
        }
    }

    /// <summary>
    /// Zeige die Nummer des Dreiecks an
    /// 
    /// Wir berechnen den Centroid des Dreiecks indem wir den Summenvektor der
    /// drei Ortsvektoren unseres Dreiecks berechnen und diesen durch 3 Teilen.
    /// Für jedes beliebige Polygon (auch n-Gon mit n Punkten) können wir diesen
    /// Mittelpunkt/Schwerpunkt berechnen, indem der Summenvektor gebildet wird
    /// und durch die Anzahl der Punkte geteilt wird.
       
    ///               
    ///            p1  
    ///             ^  
    ///            / \  
    ///           /   \ 
    ///          /  ^  \  
    ///        p0------>p2
    /// 
    /// 
    /// </summary>
    private void ShowTriangleNumbers()
    {
        // Laufe das Triangle-Array durch und betrachte in jedem Durchlauf
        // 3 Indices in diesem Array  -->  i += 3
        for (int i = 0; i < this.mesh.triangles.Length; i += 3)
        {
            // Die Indices der Vertices des Dreiecks bekommen wir aus dem 
            // Triangles-Array, da hier die Indices als Integer gespeichert sind
            // Den Index verwenden wir dann dazu uns die Position des Vertex
            // aus dem Vertices-Array zu holen.
            // Da wir in jedem Schleifendurchlauf i um 3 erhöhen, können wir
            // über i, i+1 und i+2 die 3 Vertices des Triangles bekommen.
            Vector3 p0 = mesh.vertices[mesh.triangles[i + 0]];
            Vector3 p1 = mesh.vertices[mesh.triangles[i + 1]];
            Vector3 p2 = mesh.vertices[mesh.triangles[i + 2]];

            // Der Centroid wird (wie oben beschrieben) berechnet
            // und von lokalen auf globale Koordinaten transformiert
            Vector3 centroid = (p0 + p1 + p2) / 3f;

            // Transformiere den Centroid von lokalen
            // auf globale Koordinaten
            centroid = t.TransformPoint(centroid);

            // Sollte der Centroid sichtbar sein, ist auch das Triangle
            // sichtbar. Nur dann Zeichnen wir das Label.
            // Die Nummer des Dreiecks ist der Aktuelle Index der Schleife
            // geteilt durch 3.
            if (PositionVisibleObjectRelated(centroid, collider))
                Handles.Label(centroid, (i / 3).ToString());
        }
    }

    /// <summary>
    /// Zeige die Indices der vom Triangle referenzierten Vertices an.
    /// 
    /// Diese Methode ist eine erweiterte Form der ShowVertexIndices-Methode,
    /// die das Problem überlagerter Vertices beheben soll.
    /// Beispielsweise sind die Eckpunkte des UnityCubes (bei gleicher Position)
    /// jeweils drei mal vorhanden (für jede anliegende Würfelfläche). Dadurch
    /// liegen die Labels der drei Vertices auf der gleichen Position und 
    /// überlagern sich zur Unlesbarkeit.
    /// 
    /// Um dies zu beheben, Berechnen wir für jedes Triangle den Centroid
    /// und danach Richtungsvektoren vom Centroid in Richtung der Eckpunkte.
    /// Diese Richtungsvektoren "verkürzen" wir, wodurch wir Punkte auf der 
    /// Dreiecksfläche bekommen, die in der Nähe der Ecken liegen.
    /// 
    /// An diesen Stellen zeichnen wir Labels für die vom Dreieck referenzier-
    /// ten Vertex-Indices.
    /// </summary>
    private void ShowTriangleVertexIndices()
    {
        // Laufe das Triangle-Array durch und betrachte in jedem Durchlauf
        // 3 Indices in diesem Array  -->  i += 3
        for (int i = 0; i < this.mesh.triangles.Length; i += 3)
        {
            // Die Indices der Vertices des Dreiecks bekommen wir aus dem 
            // Triangles-Array, da hier die Indices als Integer gespeichert sind
            // Den Index verwenden wir dann dazu uns die Position des Vertex
            // aus dem Vertices-Array zu holen.
            // Da wir in jedem Schleifendurchlauf i um 3 erhöhen, können wir
            // über i, i+1 und i+2 die 3 Vertices des Triangles bekommen.
            Vector3 p0 = mesh.vertices[mesh.triangles[i + 0]];
            Vector3 p1 = mesh.vertices[mesh.triangles[i + 1]];
            Vector3 p2 = mesh.vertices[mesh.triangles[i + 2]];

            // Der Centroid wird (wie oben beschrieben) berechnet
            // und von lokalen auf globale Koordinaten transformiert
            Vector3 centroid = (p0 + p1 + p2) / 3f;

            // Berechne den Richtungsvektor, der vom centroid in Richtung p0
            // zeigt, verkürzen diesen indem wir ihn mit 0.6 multiplizieren
            // und erhalten so einen Punkt auf dem Dreieck der in der Nähe
            // von p0 liegt.
            // So verfahren wir für alle 3 Punkte.
            Vector3 p0LabelPosition = centroid + (p0 - centroid) * 0.6f;
            Vector3 p1LabelPosition = centroid + (p1 - centroid) * 0.6f;
            Vector3 p2LabelPosition = centroid + (p2 - centroid) * 0.6f;

            // Transformiere die Positionen von lokalen
            // auf globale Koordinaten
            centroid =  t.TransformPoint(centroid);
            p0LabelPosition = t.TransformPoint(p0LabelPosition);
            p1LabelPosition = t.TransformPoint(p1LabelPosition);
            p2LabelPosition = t.TransformPoint(p2LabelPosition);

            // Sollte der Centroid sichtbar sein...
            if (PositionVisibleObjectRelated(centroid, collider))
            {
                // ... zeichnen wir als Labels die Indices die vom Triangle
                //     referenziert werden. 
                Handles.Label(
                    p0LabelPosition, 
                    mesh.triangles[i + 0].ToString());
                Handles.Label(
                    p1LabelPosition, 
                    mesh.triangles[i + 1].ToString());
                Handles.Label(
                    p2LabelPosition, 
                    mesh.triangles[i + 2].ToString());
            }
        }
    }

    /// <summary>
    /// Zeige die UV-Koordinaten der vom Triangle referenzierten Vertices an.
    /// 
    /// Diese Methode ist eine erweiterte Form der ShowVertexIndices-Methode,
    /// die das Problem überlagerter Vertices beheben soll.
    /// Beispielsweise sind die Eckpunkte des UnityCubes (bei gleicher Position)
    /// jeweils drei mal vorhanden (für jede anliegende Würfelfläche). Dadurch
    /// liegen die Labels der drei Vertices auf der gleichen Position und 
    /// überlagern sich zur Unlesbarkeit.
    /// 
    /// Um dies zu beheben, Berechnen wir für jedes Triangle den Centroid
    /// und danach Richtungsvektoren vom Centroid in Richtung der Eckpunkte.
    /// Diese Richtungsvektoren "verkürzen" wir, wodurch wir Punkte auf der 
    /// Dreiecksfläche bekommen, die in der Nähe der Ecken liegen.
    /// 
    /// An diesen Stellen zeichnen wir Labels für die vom Dreieck referenzier-
    /// ten Vertices bzw. deren UV-Koordinaten.
    /// </summary>
    private void ShowTriangleUVs()
    {
        // Laufe das Triangle-Array durch und betrachte in jedem Durchlauf
        // 3 Indices in diesem Array  -->  i += 3
        for (int i = 0; i < this.mesh.triangles.Length; i += 3)
        {
            // Die Indices der Vertices des Dreiecks bekommen wir aus dem 
            // Triangles-Array, da hier die Indices als Integer gespeichert sind
            // Den Index verwenden wir dann dazu uns die Position des Vertex
            // aus dem Vertices-Array zu holen.
            // Da wir in jedem Schleifendurchlauf i um 3 erhöhen, können wir
            // über i, i+1 und i+2 die 3 Vertices des Triangles bekommen.
            Vector3 p0 = mesh.vertices[mesh.triangles[i + 0]];
            Vector3 p1 = mesh.vertices[mesh.triangles[i + 1]];
            Vector3 p2 = mesh.vertices[mesh.triangles[i + 2]];

            // Der Centroid wird (wie oben beschrieben) berechnet
            // und von lokalen auf globale Koordinaten transformiert
            Vector3 centroid = (p0 + p1 + p2) / 3f;

            Vector3 p0LabelPosition = centroid + (p0 - centroid) * 0.6f;
            Vector3 p1LabelPosition = centroid + (p1 - centroid) * 0.6f;
            Vector3 p2LabelPosition = centroid + (p2 - centroid) * 0.6f;

            // Transformiere die Positionen von lokalen
            // auf globale Koordinaten
            centroid = t.TransformPoint(centroid);
            p0LabelPosition = t.TransformPoint(p0LabelPosition);
            p1LabelPosition = t.TransformPoint(p1LabelPosition);
            p2LabelPosition = t.TransformPoint(p2LabelPosition);

            // Sollte der Centroid sichtbar sein...
            if (PositionVisibleObjectRelated(centroid, collider))
            {
                // ... zeichnen wir die Labels für die UV-Koordinaten
                //     die wir uns über die vom Triangle referenzierten
                //     Vertex-Indices aus dem UV-Array holen können.
                Handles.Label(
                    p0LabelPosition, 
                    mesh.uv[mesh.triangles[i + 0]].ToString());
                Handles.Label(
                    p1LabelPosition, 
                    mesh.uv[mesh.triangles[i + 1]].ToString());
                Handles.Label(
                    p2LabelPosition, 
                    mesh.uv[mesh.triangles[i + 2]].ToString());
            }
        }
    }
    #endregion // Mesh-Info Darstellungs-Methoden

    /// <summary>
    /// Diese Methode überschreibt den Standard-Inspector für alle Simple-
    /// MeshDebugger-Komponenten.
    /// 
    /// Ist diese Methode vorhanden (und die Methode base.DrawDefault-
    /// Inspector() entfernt), wird erstmal nichts mehr im Inspector 
    /// angezeigt. 
    /// 
    /// Der DefaultInspector zeichnet ansonsten alle "public"-Felder die 
    /// im SimpleMeshDebugger vorhanden sind mit den für den Datentyp
    /// vorgesehenen GUI-Element.
    /// 
    /// Da beim DefaultInspector zwar die Werte geändert werden können, 
    /// aber nicht auf deren Änderung aktiv reagiert werden kann (z.B.
    /// um das Mesh neu zu generieren, das Szenenfenster neu zu zeichnen
    /// etc.), müssen/können wir die Methode OnInspector-GUI überschrei-
    /// ben und haben so die Möglichkeit alle Eingaben nach unseren Wün-
    /// schen zu behandeln.
    /// 
    /// Zusätzlich haben wir hier die Möglichkeit weitere "tiefer" siztende
    /// Informationen z.B. des Meshs als Labels bzw. LabelFields auszugeben.
    /// </summary>
    public override void OnInspectorGUI()
    {
        // Diese Methode stellt den Default-Inspector dar. Sollten wir
        // zusätzlich zum überschreiben der Inspector-Dartstellung wünschen
        // "auf die Schnelle" auf public-Variablen des MeshDebuggers zuzugrei-
        // fen, können wir den Default-Inspector anhängen oder vorlagern.
        // base.DrawDefaultInspector();

        // wir überprüfen zuerst, ob eine Mesh-Instanz vorhanden ist und
        // beenden den Ablauf, falls keine vorhanden ist.
        if (this.mesh == null)
            return;

        // Wir beginnen einen GUI-Bereich der auf Änderungen überwacht werden
        // soll. Wir an einem der in diesem Bereich folgenden GUI-Elemente
        // eine Änderung vorgenommen, können wir dies später prüfen und 
        // darauf mit einer Behandlung reagieren.
        EditorGUI.BeginChangeCheck();

        // Zuerst stellen wir ein paar Infos zum Mesh dar...
        EditorGUILayout.LabelField("Anzahl der Vertices", mesh.vertexCount.ToString());
        EditorGUILayout.LabelField("Anzahl an Triangles", (mesh.triangles.Length / 3).ToString());

        // Nun folgen die Check-Boxen für die Mesh-Informationen die wir
        // an und ausschalten können möchten. 
        // Hierzu können wir von der Klasse EditorGUILayout das Element Toggle
        // oder ToggleLeft verwenden. ToggleLeft bietet sich bei längeren
        // Labels zur Checkbox an, da andernfalls die standard Spalten des
        // Inspectors die Labels abschneiden können.
        //
        // Wir Zeichnen Checkboxen für alle bool-Variablen, die wir zuvor 
        // definiert haben:
        showVertexPosition = EditorGUILayout.ToggleLeft("Show Vertex Positions", showVertexPosition);
        showVertexIndices = EditorGUILayout.ToggleLeft("Show Vertex Indices", showVertexIndices);
        showVertexNormals = EditorGUILayout.ToggleLeft("Show Vertex Normals", showVertexNormals);
        showUVs = EditorGUILayout.ToggleLeft("Show UVs", showUVs);

        showTriangleNormals = EditorGUILayout.ToggleLeft("Show Triangle Normals", showTriangleNormals);
        showTriangleNumbers = EditorGUILayout.ToggleLeft("Show Triangle Numbers", showTriangleNumbers);
        showTriangleVertexIndices = EditorGUILayout.ToggleLeft("Show Triangle Vertex Indices", showTriangleVertexIndices);
        showTriangleUVs = EditorGUILayout.ToggleLeft("Show Triangle UVs", showTriangleUVs);
        
        // Sollte eine Eingabe bei einem der Elemente gemacht worden sein, 
        // erfahren wir dies über die Methode EndChangeCheck(). Diese liefert
        // true, falls eine Eingabe gemacht wurde...
        if (EditorGUI.EndChangeCheck())
            // ... und wir reagieren in diesem Fall damit, dass wir das
            //     Szenenfenster neu zeichnen lassen, damit die neu aktivierten
            //     Labels auch dargestellt werden.
            SceneView.RepaintAll();
    }
}
