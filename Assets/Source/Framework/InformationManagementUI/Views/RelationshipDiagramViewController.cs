using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using UniRx;
using System.Linq;

namespace InformationManagementUI
{
    /// <summary>
    /// View controller for the relationship diagram component
    /// </summary>
    public class RelationshipDiagramViewController : MonoBehaviour
    {
        [SerializeField] private VisualTreeAsset _characterNodeTemplate;
        [SerializeField] private VisualTreeAsset _relationshipLineTemplate;
        [SerializeField] private VisualTreeAsset _groupBoundaryTemplate;
        [SerializeField] private VisualTreeAsset _characterInfoCardTemplate;
        [SerializeField] private VisualTreeAsset _relationshipDetailCardTemplate;
        
        private RelationshipDiagramViewModel _viewModel;
        private VisualElement _rootElement;
        private VisualElement _graphViewport;
        private VisualElement _controlPanel;
        private VisualElement _detailPanel;
        
        // Graph elements containers
        private VisualElement _nodesContainer;
        private VisualElement _linesContainer;
        private VisualElement _groupsContainer;
        
        // Control elements
        private TextField _searchField;
        private DropdownField _characterTypeFilter;
        private DropdownField _relationshipTypeFilter;
        private Slider _zoomSlider;
        private Button _resetViewButton;
        private Button _focusModeToggle;
        
        // Node manipulation state
        private bool _isDragging = false;
        private Vector2 _dragStartPosition;
        private CharacterNode _draggedNode;
        private Vector2 _nodeDragOffset;
        
        // Graph state
        private Vector2 _viewportSize;
        private Dictionary<string, VisualElement> _characterNodeElements = new Dictionary<string, VisualElement>();
        private Dictionary<string, VisualElement> _relationshipLineElements = new Dictionary<string, VisualElement>();
        private Dictionary<string, VisualElement> _groupBoundaryElements = new Dictionary<string, VisualElement>();
        
        // CompositeDisposable for cleanup
        private CompositeDisposable _disposables = new CompositeDisposable();
        
        // Constants
        private const string GRAPH_VIEWPORT_NAME = "graph-viewport";
        private const string CONTROL_PANEL_NAME = "control-panel";
        private const string DETAIL_PANEL_NAME = "detail-panel";
        private const string NODES_CONTAINER_NAME = "character-nodes-container";
        private const string LINES_CONTAINER_NAME = "relationship-lines-container";
        private const string GROUPS_CONTAINER_NAME = "group-boundaries-container";
        private const string SEARCH_FIELD_NAME = "search-field";
        private const string CHARACTER_TYPE_FILTER_NAME = "character-type-filter";
        private const string RELATIONSHIP_TYPE_FILTER_NAME = "relationship-type-filter";
        private const string ZOOM_SLIDER_NAME = "zoom-slider";
        private const string RESET_VIEW_BUTTON_NAME = "reset-view-button";
        private const string FOCUS_MODE_TOGGLE_NAME = "focus-mode-toggle";
        private const string CLOSE_DETAIL_BUTTON_NAME = "close-detail-button";
        private const float NODE_SIZE = 80f;
        private const float LINE_THICKNESS = 3f;
        
        /// <summary>
        /// Initializes the view controller with a view model
        /// </summary>
        /// <param name="viewModel">The view model to initialize with</param>
        public void Initialize(RelationshipDiagramViewModel viewModel)
        {
            _viewModel = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
            
            // Get the root element (assuming this component is on the same GameObject as the UIDocument)
            var document = GetComponent<UIDocument>();
            if (document != null)
            {
                _rootElement = document.rootVisualElement;
                if (_rootElement != null)
                {
                    SetupUI();
                    BindViewModel();
                }
            }
        }
        
        /// <summary>
        /// Shows the relationship diagram view
        /// </summary>
        public void Show()
        {
            if (_rootElement != null)
            {
                _rootElement.style.display = DisplayStyle.Flex;
                
                // Refresh the graph when showing
                RefreshGraph();
            }
        }
        
        /// <summary>
        /// Hides the relationship diagram view
        /// </summary>
        public void Hide()
        {
            if (_rootElement != null)
            {
                _rootElement.style.display = DisplayStyle.None;
            }
        }
        
        private void OnDestroy()
        {
            _disposables.Dispose();
        }
        
        /// <summary>
        /// Sets up the UI elements and event handlers
        /// </summary>
        private void SetupUI()
        {
            // Get main UI containers
            _graphViewport = _rootElement.Q<VisualElement>(GRAPH_VIEWPORT_NAME);
            _controlPanel = _rootElement.Q<VisualElement>(CONTROL_PANEL_NAME);
            _detailPanel = _rootElement.Q<VisualElement>(DETAIL_PANEL_NAME);
            
            // Store viewport size
            _viewportSize = new Vector2(_graphViewport.resolvedStyle.width, _graphViewport.resolvedStyle.height);
            
            // Create graph element containers if they don't exist
            _nodesContainer = _graphViewport.Q<VisualElement>(NODES_CONTAINER_NAME);
            if (_nodesContainer == null)
            {
                _nodesContainer = new VisualElement { name = NODES_CONTAINER_NAME };
                _nodesContainer.AddToClassList("nodes-container");
                _graphViewport.Add(_nodesContainer);
            }
            
            _linesContainer = _graphViewport.Q<VisualElement>(LINES_CONTAINER_NAME);
            if (_linesContainer == null)
            {
                _linesContainer = new VisualElement { name = LINES_CONTAINER_NAME };
                _linesContainer.AddToClassList("lines-container");
                _graphViewport.Add(_linesContainer);
            }
            
            _groupsContainer = _graphViewport.Q<VisualElement>(GROUPS_CONTAINER_NAME);
            if (_groupsContainer == null)
            {
                _groupsContainer = new VisualElement { name = GROUPS_CONTAINER_NAME };
                _groupsContainer.AddToClassList("groups-container");
                _graphViewport.Add(_groupsContainer);
            }
            
            // Ensure proper rendering order
            _groupsContainer.BringToFront();
            _linesContainer.BringToFront();
            _nodesContainer.BringToFront();
            
            // Get control elements
            _searchField = _controlPanel.Q<TextField>(SEARCH_FIELD_NAME);
            _characterTypeFilter = _controlPanel.Q<DropdownField>(CHARACTER_TYPE_FILTER_NAME);
            _relationshipTypeFilter = _controlPanel.Q<DropdownField>(RELATIONSHIP_TYPE_FILTER_NAME);
            _zoomSlider = _controlPanel.Q<Slider>(ZOOM_SLIDER_NAME);
            _resetViewButton = _controlPanel.Q<Button>(RESET_VIEW_BUTTON_NAME);
            _focusModeToggle = _controlPanel.Q<Button>(FOCUS_MODE_TOGGLE_NAME);
            
            // Set up control elements
            SetupControlElements();
            
            // Set up detail panel
            SetupDetailPanel();
            
            // Set up graph viewport events
            SetupGraphViewportEvents();
        }
        
        /// <summary>
        /// Sets up the control panel elements
        /// </summary>
        private void SetupControlElements()
        {
            // Set up search field
            if (_searchField != null)
            {
                _searchField.RegisterCallback<ChangeEvent<string>>(evt => 
                {
                    _viewModel.SetSearchQuery(evt.newValue);
                });
            }
            
            // Set up character type filter
            if (_characterTypeFilter != null)
            {
                // Populate with character types
                List<string> characterTypes = new List<string> { "All" };
                foreach (CharacterType type in Enum.GetValues(typeof(CharacterType)))
                {
                    characterTypes.Add(type.ToString());
                }
                _characterTypeFilter.choices = characterTypes;
                _characterTypeFilter.value = "All";
                
                _characterTypeFilter.RegisterValueChangedCallback(evt => 
                {
                    if (evt.newValue == "All")
                    {
                        _viewModel.FilterCharacterTypes(null);
                    }
                    else
                    {
                        _viewModel.FilterCharacterTypes(new List<string> { evt.newValue });
                    }
                });
            }
            
            // Set up relationship type filter
            if (_relationshipTypeFilter != null)
            {
                // Populate with relationship types
                List<string> relationshipTypes = new List<string> { "All" };
                foreach (RelationshipType type in Enum.GetValues(typeof(RelationshipType)))
                {
                    relationshipTypes.Add(type.ToString());
                }
                _relationshipTypeFilter.choices = relationshipTypes;
                _relationshipTypeFilter.value = "All";
                
                _relationshipTypeFilter.RegisterValueChangedCallback(evt => 
                {
                    if (evt.newValue == "All")
                    {
                        _viewModel.FilterRelationshipTypes(null);
                    }
                    else
                    {
                        RelationshipType type = (RelationshipType)Enum.Parse(typeof(RelationshipType), evt.newValue);
                        _viewModel.FilterRelationshipTypes(new List<RelationshipType> { type });
                    }
                });
            }
            
            // Set up zoom slider
            if (_zoomSlider != null)
            {
                _zoomSlider.lowValue = 0.5f;
                _zoomSlider.highValue = 2.0f;
                _zoomSlider.value = 1.0f;
                
                _zoomSlider.RegisterValueChangedCallback(evt => 
                {
                    _viewModel.SetZoomLevel(evt.newValue);
                });
            }
            
            // Set up reset view button
            if (_resetViewButton != null)
            {
                _resetViewButton.clicked += () => 
                {
                    _viewModel.ResetView();
                };
            }
            
            // Set up focus mode toggle
            if (_focusModeToggle != null)
            {
                _focusModeToggle.clicked += () => 
                {
                    if (string.IsNullOrEmpty(_viewModel.FocusCharacterId.Value))
                    {
                        // If a character is selected, focus on it
                        if (_viewModel.SelectedCharacter.Value != null)
                        {
                            _viewModel.SetFocusCharacter(_viewModel.SelectedCharacter.Value.CharacterID);
                            _focusModeToggle.AddToClassList("active-button");
                        }
                    }
                    else
                    {
                        // Clear focus mode
                        _viewModel.SetFocusCharacter(string.Empty);
                        _focusModeToggle.RemoveFromClassList("active-button");
                    }
                };
            }
        }
        
        /// <summary>
        /// Sets up the detail panel for showing character and relationship information
        /// </summary>
        private void SetupDetailPanel()
        {
            // Initially hide the detail panel
            if (_detailPanel != null)
            {
                _detailPanel.style.display = DisplayStyle.None;
                
                // Set up close button
                var closeButton = _detailPanel.Q<Button>(CLOSE_DETAIL_BUTTON_NAME);
                if (closeButton != null)
                {
                    closeButton.clicked += () => 
                    {
                        _detailPanel.style.display = DisplayStyle.None;
                        _viewModel.SelectedCharacter.Value = null;
                        _viewModel.SelectedRelationship.Value = null;
                    };
                }
            }
        }
        
        /// <summary>
        /// Sets up event handlers for the graph viewport
        /// </summary>
        private void SetupGraphViewportEvents()
        {
            if (_graphViewport == null) return;
            
            // Register for mouse events on the viewport
            _graphViewport.RegisterCallback<MouseDownEvent>(OnViewportMouseDown);
            _graphViewport.RegisterCallback<MouseMoveEvent>(OnViewportMouseMove);
            _graphViewport.RegisterCallback<MouseUpEvent>(OnViewportMouseUp);
            _graphViewport.RegisterCallback<WheelEvent>(OnViewportWheel);
            
            // Register for viewport size changes
            _graphViewport.RegisterCallback<GeometryChangedEvent>(evt => 
            {
                _viewportSize = new Vector2(evt.newRect.width, evt.newRect.height);
                RefreshGraph();
            });
        }
        
        /// <summary>
        /// Binds the view model to the UI
        /// </summary>
        private void BindViewModel()
        {
            if (_viewModel == null) return;
            
            // Bind character collection changes
            _viewModel.FilteredCharacters.ObserveCountChanged()
                .Subscribe(_ => RefreshGraph())
                .AddTo(_disposables);
            
            // Bind relationship collection changes
            _viewModel.FilteredRelationships.ObserveCountChanged()
                .Subscribe(_ => RefreshRelationships())
                .AddTo(_disposables);
            
            // Bind zoom level changes
            _viewModel.ZoomLevel
                .Subscribe(zoom => 
                {
                    if (_zoomSlider != null && Math.Abs(_zoomSlider.value - zoom) > 0.01f)
                    {
                        _zoomSlider.value = zoom;
                    }
                    ApplyZoomLevel(zoom);
                })
                .AddTo(_disposables);
            
            // Bind pan position changes
            _viewModel.PanPosition
                .Subscribe(pos => ApplyPanPosition(pos))
                .AddTo(_disposables);
            
            // Bind selected character changes
            _viewModel.SelectedCharacter
                .Subscribe(character => ShowCharacterDetails(character))
                .AddTo(_disposables);
            
            // Bind selected relationship changes
            _viewModel.SelectedRelationship
                .Subscribe(relationship => ShowRelationshipDetails(relationship))
                .AddTo(_disposables);
            
            // Bind search query changes
            _viewModel.SearchQuery
                .Subscribe(query => 
                {
                    if (_searchField != null && _searchField.value != query)
                    {
                        _searchField.value = query;
                    }
                })
                .AddTo(_disposables);
            
            // Bind focus character changes
            _viewModel.FocusCharacterId
                .Subscribe(id => 
                {
                    if (_focusModeToggle != null)
                    {
                        if (string.IsNullOrEmpty(id))
                        {
                            _focusModeToggle.RemoveFromClassList("active-button");
                        }
                        else
                        {
                            _focusModeToggle.AddToClassList("active-button");
                        }
                    }
                    
                    // Update highlights when focus changes
                    UpdateNodeHighlights();
                })
                .AddTo(_disposables);
            
            // Initial graph refresh
            RefreshGraph();
        }
        
        /// <summary>
        /// Refreshes the graph with the current data
        /// </summary>
        private void RefreshGraph()
        {
            if (_viewModel == null) return;
            
            RefreshGroups();
            RefreshCharacterNodes();
            RefreshRelationships();
            
            // Apply current zoom and pan
            ApplyZoomLevel(_viewModel.ZoomLevel.Value);
            ApplyPanPosition(_viewModel.PanPosition.Value);
            
            // Update node highlights
            UpdateNodeHighlights();
        }
        
        /// <summary>
        /// Refreshes the character nodes in the graph
        /// </summary>
        private void RefreshCharacterNodes()
        {
            if (_viewModel == null || _nodesContainer == null) return;
            
            // Keep track of existing nodes for cleanup
            HashSet<string> existingNodeIds = new HashSet<string>(_characterNodeElements.Keys);
            
            // Add or update nodes
            foreach (var character in _viewModel.FilteredCharacters)
            {
                existingNodeIds.Remove(character.CharacterID);
                
                if (_characterNodeElements.TryGetValue(character.CharacterID, out var nodeElement))
                {
                    // Update existing node
                    UpdateCharacterNode(nodeElement, character);
                }
                else
                {
                    // Create new node
                    nodeElement = CreateCharacterNode(character);
                    _characterNodeElements[character.CharacterID] = nodeElement;
                    _nodesContainer.Add(nodeElement);
                }
                
                // Position the node
                PositionCharacterNode(nodeElement, character);
            }
            
            // Remove nodes that are no longer in the filtered list
            foreach (var nodeId in existingNodeIds)
            {
                if (_characterNodeElements.TryGetValue(nodeId, out var nodeElement))
                {
                    nodeElement.RemoveFromHierarchy();
                    _characterNodeElements.Remove(nodeId);
                }
            }
        }
        
        /// <summary>
        /// Creates a character node UI element
        /// </summary>
        /// <param name="character">The character data</param>
        /// <returns>The character node UI element</returns>
        private VisualElement CreateCharacterNode(CharacterNode character)
        {
            var nodeElement = _characterNodeTemplate.Instantiate()[0];
            nodeElement.name = $"node-{character.CharacterID}";
            nodeElement.userData = character; // Store character data in userData for easy access
            
            // Set up node content
            var nameLabel = nodeElement.Q<Label>("character-name");
            if (nameLabel != null)
            {
                nameLabel.text = character.Name;
            }
            
            var portraitImage = nodeElement.Q<VisualElement>("portrait-image");
            if (portraitImage != null && character.Portrait != null)
            {
                var backgroundImage = new StyleBackground(character.Portrait);
                portraitImage.style.backgroundImage = backgroundImage;
            }
            
            var statusIndicator = nodeElement.Q<VisualElement>("status-indicator");
            if (statusIndicator != null)
            {
                ApplyCharacterTypeStyle(statusIndicator, character.Type);
            }
            
            // Add mouse event handlers for the node
            nodeElement.RegisterCallback<MouseDownEvent>(evt => OnNodeMouseDown(evt, character));
            nodeElement.RegisterCallback<MouseUpEvent>(evt => OnNodeMouseUp(evt, character));
            nodeElement.RegisterCallback<ClickEvent>(evt => OnNodeClick(evt, character));
            
            return nodeElement;
        }
        
        /// <summary>
        /// Updates an existing character node UI element
        /// </summary>
        /// <param name="nodeElement">The node element to update</param>
        /// <param name="character">The character data</param>
        private void UpdateCharacterNode(VisualElement nodeElement, CharacterNode character)
        {
            // Update userData
            nodeElement.userData = character;
            
            // Update name
            var nameLabel = nodeElement.Q<Label>("character-name");
            if (nameLabel != null)
            {
                nameLabel.text = character.Name;
            }
            
            // Update portrait
            var portraitImage = nodeElement.Q<VisualElement>("portrait-image");
            if (portraitImage != null && character.Portrait != null)
            {
                var backgroundImage = new StyleBackground(character.Portrait);
                portraitImage.style.backgroundImage = backgroundImage;
            }
            
            // Update status indicator
            var statusIndicator = nodeElement.Q<VisualElement>("status-indicator");
            if (statusIndicator != null)
            {
                ApplyCharacterTypeStyle(statusIndicator, character.Type);
            }
        }
        
        /// <summary>
        /// Positions a character node UI element
        /// </summary>
        /// <param name="nodeElement">The node element to position</param>
        /// <param name="character">The character data</param>
        private void PositionCharacterNode(VisualElement nodeElement, CharacterNode character)
        {
            // Convert graph coordinates to screen coordinates
            Vector2 screenPosition = GraphToScreenPosition(character.Position);
            
            // Position the node
            nodeElement.style.left = screenPosition.x - (NODE_SIZE / 2);
            nodeElement.style.top = screenPosition.y - (NODE_SIZE / 2);
        }
        
        /// <summary>
        /// Refreshes the relationship lines in the graph
        /// </summary>
        private void RefreshRelationships()
        {
            if (_viewModel == null || _linesContainer == null) return;
            
            // Keep track of existing lines for cleanup
            HashSet<string> existingLineIds = new HashSet<string>(_relationshipLineElements.Keys);
            
            // Add or update lines
            foreach (var relationship in _viewModel.FilteredRelationships)
            {
                // Generate a unique ID for the relationship
                string relationshipId = $"{relationship.SourceCharacterID}:{relationship.TargetCharacterID}";
                existingLineIds.Remove(relationshipId);
                
                // Get source and target nodes
                CharacterNode sourceNode = null;
                CharacterNode targetNode = null;
                
                foreach (var character in _viewModel.FilteredCharacters)
                {
                    if (character.CharacterID == relationship.SourceCharacterID)
                    {
                        sourceNode = character;
                    }
                    else if (character.CharacterID == relationship.TargetCharacterID)
                    {
                        targetNode = character;
                    }
                    
                    if (sourceNode != null && targetNode != null) break;
                }
                
                // Skip if either node is not visible
                if (sourceNode == null || targetNode == null) continue;
                
                if (_relationshipLineElements.TryGetValue(relationshipId, out var lineElement))
                {
                    // Update existing line
                    UpdateRelationshipLine(lineElement, relationship, sourceNode, targetNode);
                }
                else
                {
                    // Create new line
                    lineElement = CreateRelationshipLine(relationship, sourceNode, targetNode);
                    _relationshipLineElements[relationshipId] = lineElement;
                    _linesContainer.Add(lineElement);
                }
            }
            
            // Remove lines that are no longer in the filtered list
            foreach (var lineId in existingLineIds)
            {
                if (_relationshipLineElements.TryGetValue(lineId, out var lineElement))
                {
                    lineElement.RemoveFromHierarchy();
                    _relationshipLineElements.Remove(lineId);
                }
            }
        }
        
        /// <summary>
        /// Creates a relationship line UI element
        /// </summary>
        /// <param name="relationship">The relationship data</param>
        /// <param name="sourceNode">The source character node</param>
        /// <param name="targetNode">The target character node</param>
        /// <returns>The relationship line UI element</returns>
        private VisualElement CreateRelationshipLine(RelationshipEdge relationship, CharacterNode sourceNode, CharacterNode targetNode)
        {
            var lineElement = _relationshipLineTemplate.Instantiate()[0];
            lineElement.name = $"line-{relationship.SourceCharacterID}-{relationship.TargetCharacterID}";
            lineElement.userData = relationship; // Store relationship data in userData for easy access
            
            // Update the line display
            UpdateRelationshipLine(lineElement, relationship, sourceNode, targetNode);
            
            // Add click handler for the line
            lineElement.RegisterCallback<ClickEvent>(evt => 
            {
                evt.StopPropagation();
                _viewModel.SelectRelationshipCommand.Execute($"{relationship.SourceCharacterID}:{relationship.TargetCharacterID}");
            });
            
            return lineElement;
        }
        
        /// <summary>
        /// Updates an existing relationship line UI element
        /// </summary>
        /// <param name="lineElement">The line element to update</param>
        /// <param name="relationship">The relationship data</param>
        /// <param name="sourceNode">The source character node</param>
        /// <param name="targetNode">The target character node</param>
        private void UpdateRelationshipLine(VisualElement lineElement, RelationshipEdge relationship, CharacterNode sourceNode, CharacterNode targetNode)
        {
            // Update userData
            lineElement.userData = relationship;
            
            // Get screen positions for nodes
            Vector2 sourcePosition = GraphToScreenPosition(sourceNode.Position);
            Vector2 targetPosition = GraphToScreenPosition(targetNode.Position);
            
            // Calculate line length and angle
            Vector2 direction = targetPosition - sourcePosition;
            float length = direction.magnitude;
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            
            // Position the line
            lineElement.style.left = sourcePosition.x;
            lineElement.style.top = sourcePosition.y;
            lineElement.style.width = length;
            lineElement.style.height = LINE_THICKNESS;
            
            // Rotate the line to point from source to target
            lineElement.style.rotate = new StyleRotate(new Rotate(angle));
            
            // Apply styling based on relationship type and strength
            var lineContent = lineElement.Q<VisualElement>("line-content");
            if (lineContent != null)
            {
                // Set opacity based on relationship strength
                lineContent.style.opacity = relationship.Strength;
                
                // Set color based on relationship type
                lineContent.style.backgroundColor = GetRelationshipTypeColor(relationship.Type);
                
                // Set line thickness based on relationship strength
                float thickness = Mathf.Lerp(1f, LINE_THICKNESS, relationship.Strength);
                lineContent.style.height = thickness;
            }
            
            // Show relationship type indicator
            var typeIndicator = lineElement.Q<VisualElement>("type-indicator");
            if (typeIndicator != null)
            {
                // Position indicator in the middle of the line
                typeIndicator.style.left = length / 2 - typeIndicator.resolvedStyle.width / 2;
                
                // Set indicator based on relationship type
                var typeIcon = typeIndicator.Q<VisualElement>("type-icon");
                if (typeIcon != null)
                {
                    ApplyRelationshipTypeStyle(typeIcon, relationship.Type);
                }
            }
        }
        
        /// <summary>
        /// Refreshes the group boundaries in the graph
        /// </summary>
        private void RefreshGroups()
        {
            if (_viewModel == null || _groupsContainer == null) return;
            
            // Keep track of existing groups for cleanup
            HashSet<string> existingGroupIds = new HashSet<string>(_groupBoundaryElements.Keys);
            
            // Add or update groups
            foreach (var group in _viewModel.Groups)
            {
                existingGroupIds.Remove(group.GroupID);
                
                // Check if this group should be visible based on filtered characters
                bool isVisible = false;
                foreach (var characterId in group.MemberIDs)
                {
                    if (_viewModel.FilteredCharacters.Any(c => c.CharacterID == characterId))
                    {
                        isVisible = true;
                        break;
                    }
                }
                
                if (!isVisible) continue;
                
                if (_groupBoundaryElements.TryGetValue(group.GroupID, out var groupElement))
                {
                    // Update existing group
                    UpdateGroupBoundary(groupElement, group);
                }
                else
                {
                    // Create new group
                    groupElement = CreateGroupBoundary(group);
                    _groupBoundaryElements[group.GroupID] = groupElement;
                    _groupsContainer.Add(groupElement);
                }
            }
            
            // Remove groups that are no longer in the filtered list
            foreach (var groupId in existingGroupIds)
            {
                if (_groupBoundaryElements.TryGetValue(groupId, out var groupElement))
                {
                    groupElement.RemoveFromHierarchy();
                    _groupBoundaryElements.Remove(groupId);
                }
            }
        }
        
        /// <summary>
        /// Creates a group boundary UI element
        /// </summary>
        /// <param name="group">The group data</param>
        /// <returns>The group boundary UI element</returns>
        private VisualElement CreateGroupBoundary(CharacterGroup group)
        {
            var groupElement = _groupBoundaryTemplate.Instantiate()[0];
            groupElement.name = $"group-{group.GroupID}";
            groupElement.userData = group; // Store group data in userData for easy access
            
            // Set up group content
            var nameLabel = groupElement.Q<Label>("group-name");
            if (nameLabel != null)
            {
                nameLabel.text = group.Name;
            }
            
            // Apply group color
            groupElement.style.borderTopColor = groupElement.style.borderRightColor = 
            groupElement.style.borderBottomColor = groupElement.style.borderLeftColor = new StyleColor(group.Color);
            
            // Add click handler for the group
            groupElement.RegisterCallback<ClickEvent>(evt => 
            {
                evt.StopPropagation();
                _viewModel.SelectGroupCommand.Execute(group.GroupID);
            });
            
            // Update the boundary display
            UpdateGroupBoundary(groupElement, group);
            
            return groupElement;
        }
        
        /// <summary>
        /// Updates an existing group boundary UI element
        /// </summary>
        /// <param name="groupElement">The group element to update</param>
        /// <param name="group">The group data</param>
        private void UpdateGroupBoundary(VisualElement groupElement, CharacterGroup group)
        {
            // Update userData
            groupElement.userData = group;
            
            // Update name
            var nameLabel = groupElement.Q<Label>("group-name");
            if (nameLabel != null)
            {
                nameLabel.text = group.Name;
            }
            
            // Apply group color
            groupElement.style.borderTopColor = groupElement.style.borderRightColor = 
            groupElement.style.borderBottomColor = groupElement.style.borderLeftColor = new StyleColor(group.Color);
            
            // Calculate boundary based on member positions
            Vector2 minPos = new Vector2(float.MaxValue, float.MaxValue);
            Vector2 maxPos = new Vector2(float.MinValue, float.MinValue);
            
            bool foundAnyMembers = false;
            
            foreach (var characterId in group.MemberIDs)
            {
                var character = _viewModel.FilteredCharacters.FirstOrDefault(c => c.CharacterID == characterId);
                if (character != null)
                {
                    foundAnyMembers = true;
                    Vector2 pos = GraphToScreenPosition(character.Position);
                    
                    // Expand boundary to include this node (with padding)
                    minPos.x = Mathf.Min(minPos.x, pos.x - NODE_SIZE / 2 - 20);
                    minPos.y = Mathf.Min(minPos.y, pos.y - NODE_SIZE / 2 - 20);
                    maxPos.x = Mathf.Max(maxPos.x, pos.x + NODE_SIZE / 2 + 20);
                    maxPos.y = Mathf.Max(maxPos.y, pos.y + NODE_SIZE / 2 + 20);
                }
            }
            
            // If no members are visible, hide the group
            if (!foundAnyMembers)
            {
                groupElement.style.display = DisplayStyle.None;
                return;
            }
            
            // Position and size the group boundary
            groupElement.style.display = DisplayStyle.Flex;
            groupElement.style.left = minPos.x;
            groupElement.style.top = minPos.y;
            groupElement.style.width = maxPos.x - minPos.x;
            groupElement.style.height = maxPos.y - minPos.y;
            
            // Position the name label at the top left
            if (nameLabel != null)
            {
                nameLabel.style.left = 10;
                nameLabel.style.top = 10;
            }
        }
        
        /// <summary>
        /// Shows details for a character in the detail panel
        /// </summary>
        /// <param name="character">The character to show details for</param>
        private void ShowCharacterDetails(CharacterNode character)
        {
            if (_detailPanel == null) return;
            
            // Clear existing content
            _detailPanel.Clear();
            
            if (character == null)
            {
                _detailPanel.style.display = DisplayStyle.None;
                return;
            }
            
            // Create character info card
            var infoCard = _characterInfoCardTemplate.Instantiate()[0];
            
            // Set character information
            var nameLabel = infoCard.Q<Label>("character-name");
            if (nameLabel != null)
            {
                nameLabel.text = character.Name;
            }
            
            var typeLabel = infoCard.Q<Label>("character-type");
            if (typeLabel != null)
            {
                typeLabel.text = character.Type.ToString();
            }
            
            var portraitImage = infoCard.Q<VisualElement>("character-portrait");
            if (portraitImage != null && character.Portrait != null)
            {
                var backgroundImage = new StyleBackground(character.Portrait);
                portraitImage.style.backgroundImage = backgroundImage;
            }
            
            // Add group memberships
            var groupsContainer = infoCard.Q<VisualElement>("group-memberships");
            if (groupsContainer != null)
            {
                groupsContainer.Clear();
                
                foreach (var groupId in character.GroupIDs)
                {
                    var group = _viewModel.Groups.FirstOrDefault(g => g.GroupID == groupId);
                    if (group != null)
                    {
                        var groupItem = new VisualElement();
                        groupItem.AddToClassList("group-item");
                        
                        var colorIndicator = new VisualElement();
                        colorIndicator.AddToClassList("group-color-indicator");
                        colorIndicator.style.backgroundColor = group.Color;
                        groupItem.Add(colorIndicator);
                        
                        var groupNameLabel = new Label(group.Name);
                        groupItem.Add(groupNameLabel);
                        
                        groupsContainer.Add(groupItem);
                    }
                }
            }
            
            // Add relationship list
            var relationshipsContainer = infoCard.Q<VisualElement>("relationships-list");
            if (relationshipsContainer != null)
            {
                relationshipsContainer.Clear();
                
                var relationships = _viewModel.GetCharacterRelationships(character.CharacterID);
                foreach (var relationship in relationships)
                {
                    // Determine the related character
                    string relatedCharacterId = relationship.SourceCharacterID == character.CharacterID 
                        ? relationship.TargetCharacterID 
                        : relationship.SourceCharacterID;
                    
                    var relatedCharacter = _viewModel.GetCharacter(relatedCharacterId);
                    if (relatedCharacter == null) continue;
                    
                    var relationshipItem = new VisualElement();
                    relationshipItem.AddToClassList("relationship-item");
                    
                    // Direction indicator (outgoing or incoming)
                    var directionIndicator = new VisualElement();
                    directionIndicator.AddToClassList("direction-indicator");
                    
                    if (relationship.SourceCharacterID == character.CharacterID)
                    {
                        directionIndicator.AddToClassList("outgoing");
                    }
                    else
                    {
                        directionIndicator.AddToClassList("incoming");
                    }
                    
                    relationshipItem.Add(directionIndicator);
                    
                    // Type indicator
                    var typeIndicator = new VisualElement();
                    typeIndicator.AddToClassList("relationship-type-indicator");
                    ApplyRelationshipTypeStyle(typeIndicator, relationship.Type);
                    relationshipItem.Add(typeIndicator);
                    
                    // Related character name
                    var relatedNameLabel = new Label(relatedCharacter.Name);
                    relationshipItem.Add(relatedNameLabel);
                    
                    // Strength indicator
                    var strengthIndicator = new VisualElement();
                    strengthIndicator.AddToClassList("strength-indicator");
                    strengthIndicator.style.width = new Length(relationship.Strength * 100, LengthUnit.Percent);
                    relationshipItem.Add(strengthIndicator);
                    
                    // Make relationship item clickable
                    relationshipItem.RegisterCallback<ClickEvent>(evt => 
                    {
                        _viewModel.SelectRelationshipCommand.Execute(
                            $"{relationship.SourceCharacterID}:{relationship.TargetCharacterID}");
                    });
                    
                    relationshipsContainer.Add(relationshipItem);
                }
            }
            
            // Add focus button
            var focusButton = infoCard.Q<Button>("focus-button");
            if (focusButton != null)
            {
                focusButton.clicked += () => 
                {
                    _viewModel.SetFocusCharacter(character.CharacterID);
                };
            }
            
            // Add close button
            var closeButton = new Button { text = "Close" };
            closeButton.name = CLOSE_DETAIL_BUTTON_NAME;
            closeButton.AddToClassList("close-button");
            closeButton.clicked += () => 
            {
                _detailPanel.style.display = DisplayStyle.None;
                _viewModel.SelectedCharacter.Value = null;
                _viewModel.SelectedRelationship.Value = null;
            };
            infoCard.Add(closeButton);
            
            // Show the detail panel with the character info
            _detailPanel.Add(infoCard);
            _detailPanel.style.display = DisplayStyle.Flex;
        }
        
        /// <summary>
        /// Shows details for a relationship in the detail panel
        /// </summary>
        /// <param name="relationship">The relationship to show details for</param>
        private void ShowRelationshipDetails(RelationshipEdge relationship)
        {
            if (_detailPanel == null) return;
            
            // Clear existing content
            _detailPanel.Clear();
            
            if (relationship == null)
            {
                // Only hide if character is also null
                if (_viewModel.SelectedCharacter.Value == null)
                {
                    _detailPanel.style.display = DisplayStyle.None;
                }
                return;
            }
            
            // Create relationship detail card
            var detailCard = _relationshipDetailCardTemplate.Instantiate()[0];
            
            // Get source and target characters
            var sourceCharacter = _viewModel.GetCharacter(relationship.SourceCharacterID);
            var targetCharacter = _viewModel.GetCharacter(relationship.TargetCharacterID);
            
            if (sourceCharacter == null || targetCharacter == null)
            {
                _detailPanel.style.display = DisplayStyle.None;
                return;
            }
            
            // Set relationship header
            var headerLabel = detailCard.Q<Label>("relationship-header");
            if (headerLabel != null)
            {
                headerLabel.text = $"{sourceCharacter.Name} → {targetCharacter.Name}";
            }
            
            // Set relationship type
            var typeLabel = detailCard.Q<Label>("relationship-type");
            if (typeLabel != null)
            {
                typeLabel.text = relationship.Type.ToString();
            }
            
            // Set relationship strength
            var strengthProgress = detailCard.Q<VisualElement>("strength-progress");
            if (strengthProgress != null)
            {
                strengthProgress.style.width = new Length(relationship.Strength * 100, LengthUnit.Percent);
            }
            
            var strengthLabel = detailCard.Q<Label>("strength-value");
            if (strengthLabel != null)
            {
                strengthLabel.text = $"{Mathf.Round(relationship.Strength * 100)}%";
            }
            
            // Set relationship attributes
            var attributesContainer = detailCard.Q<VisualElement>("attributes-list");
            if (attributesContainer != null && relationship.Attributes.Count > 0)
            {
                attributesContainer.Clear();
                
                foreach (var attribute in relationship.Attributes)
                {
                    var attributeItem = new VisualElement();
                    attributeItem.AddToClassList("attribute-item");
                    
                    var attrNameLabel = new Label(attribute.Type.ToString());
                    attributeItem.Add(attrNameLabel);
                    
                    var attrValueProgress = new VisualElement();
                    attrValueProgress.AddToClassList("attribute-value-progress");
                    
                    var attrValueFill = new VisualElement();
                    attrValueFill.AddToClassList("attribute-value-fill");
                    attrValueFill.style.width = new Length(attribute.Value * 100, LengthUnit.Percent);
                    
                    attrValueProgress.Add(attrValueFill);
                    attributeItem.Add(attrValueProgress);
                    
                    var attrValueLabel = new Label($"{Mathf.Round(attribute.Value * 100)}%");
                    attributeItem.Add(attrValueLabel);
                    
                    attributesContainer.Add(attributeItem);
                }
            }
            
            // Add character portraits
            var sourcePortrait = detailCard.Q<VisualElement>("source-portrait");
            if (sourcePortrait != null && sourceCharacter.Portrait != null)
            {
                var backgroundImage = new StyleBackground(sourceCharacter.Portrait);
                sourcePortrait.style.backgroundImage = backgroundImage;
            }
            
            var targetPortrait = detailCard.Q<VisualElement>("target-portrait");
            if (targetPortrait != null && targetCharacter.Portrait != null)
            {
                var backgroundImage = new StyleBackground(targetCharacter.Portrait);
                targetPortrait.style.backgroundImage = backgroundImage;
            }
            
            // Add strength edit controls
            var increaseButton = detailCard.Q<Button>("increase-strength");
            var decreaseButton = detailCard.Q<Button>("decrease-strength");
            
            if (increaseButton != null)
            {
                increaseButton.clicked += () => 
                {
                    float newStrength = Mathf.Min(1.0f, relationship.Strength + 0.1f);
                    var updatedRelationship = new RelationshipEdge(
                        relationship.SourceCharacterID,
                        relationship.TargetCharacterID,
                        relationship.Type,
                        newStrength,
                        new List<RelationshipAttribute>(relationship.Attributes)
                    );
                    _viewModel.AddOrUpdateRelationship(updatedRelationship);
                };
            }
            
            if (decreaseButton != null)
            {
                decreaseButton.clicked += () => 
                {
                    float newStrength = Mathf.Max(0.0f, relationship.Strength - 0.1f);
                    var updatedRelationship = new RelationshipEdge(
                        relationship.SourceCharacterID,
                        relationship.TargetCharacterID,
                        relationship.Type,
                        newStrength,
                        new List<RelationshipAttribute>(relationship.Attributes)
                    );
                    _viewModel.AddOrUpdateRelationship(updatedRelationship);
                };
            }
            
            // Add focus buttons
            var focusSourceButton = detailCard.Q<Button>("focus-source");
            var focusTargetButton = detailCard.Q<Button>("focus-target");
            
            if (focusSourceButton != null)
            {
                focusSourceButton.clicked += () => 
                {
                    _viewModel.SelectCharacterCommand.Execute(sourceCharacter.CharacterID);
                };
            }
            
            if (focusTargetButton != null)
            {
                focusTargetButton.clicked += () => 
                {
                    _viewModel.SelectCharacterCommand.Execute(targetCharacter.CharacterID);
                };
            }
            
            // Add close button
            var closeButton = new Button { text = "Close" };
            closeButton.name = CLOSE_DETAIL_BUTTON_NAME;
            closeButton.AddToClassList("close-button");
            closeButton.clicked += () => 
            {
                _detailPanel.style.display = DisplayStyle.None;
                _viewModel.SelectedCharacter.Value = null;
                _viewModel.SelectedRelationship.Value = null;
            };
            detailCard.Add(closeButton);
            
            // Show the detail panel with the relationship details
            _detailPanel.Add(detailCard);
            _detailPanel.style.display = DisplayStyle.Flex;
        }
        
        /// <summary>
        /// Applies zoom level to the graph
        /// </summary>
        /// <param name="zoomLevel">The zoom level to apply</param>
        private void ApplyZoomLevel(float zoomLevel)
        {
            if (_nodesContainer == null || _linesContainer == null || _groupsContainer == null) return;

            Vector2 zoom = new Vector2(zoomLevel, zoomLevel);
            // Apply zoom scale to graph containers
            _nodesContainer.style.scale = new StyleScale(new Scale(zoom));
            _linesContainer.style.scale = new StyleScale(new Scale(zoom));
            _groupsContainer.style.scale = new StyleScale(new Scale(zoom));
        }
        
        /// <summary>
        /// Applies pan position to the graph
        /// </summary>
        /// <param name="panPosition">The pan position to apply</param>
        private void ApplyPanPosition(Vector2 panPosition)
        {
            if (_nodesContainer == null || _linesContainer == null || _groupsContainer == null) return;
            
            // Apply pan position to graph containers
            _nodesContainer.style.translate = new StyleTranslate(new Translate(panPosition.x, panPosition.y));
            _linesContainer.style.translate = new StyleTranslate(new Translate(panPosition.x, panPosition.y));
            _groupsContainer.style.translate = new StyleTranslate(new Translate(panPosition.x, panPosition.y));
        }
        
        /// <summary>
        /// Updates the highlights of character nodes
        /// </summary>
        private void UpdateNodeHighlights()
        {
            if (_viewModel == null) return;
            
            // Clear highlights on all nodes
            foreach (var nodeElement in _characterNodeElements.Values)
            {
                nodeElement.RemoveFromClassList("selected-node");
                nodeElement.RemoveFromClassList("focus-node");
                nodeElement.RemoveFromClassList("related-node");
            }
            
            // Clear highlights on all relationship lines
            foreach (var lineElement in _relationshipLineElements.Values)
            {
                lineElement.RemoveFromClassList("selected-relationship");
                lineElement.RemoveFromClassList("focus-relationship");
            }
            
            // Apply selected node highlight
            if (_viewModel.SelectedCharacter.Value != null)
            {
                string characterId = _viewModel.SelectedCharacter.Value.CharacterID;
                if (_characterNodeElements.TryGetValue(characterId, out var nodeElement))
                {
                    nodeElement.AddToClassList("selected-node");
                }
            }
            
            // Apply selected relationship highlight
            if (_viewModel.SelectedRelationship.Value != null)
            {
                string relationshipId = $"{_viewModel.SelectedRelationship.Value.SourceCharacterID}:{_viewModel.SelectedRelationship.Value.TargetCharacterID}";
                if (_relationshipLineElements.TryGetValue(relationshipId, out var lineElement))
                {
                    lineElement.AddToClassList("selected-relationship");
                }
            }
            
            // Apply focus highlights if in focus mode
            if (!string.IsNullOrEmpty(_viewModel.FocusCharacterId.Value))
            {
                string focusCharacterId = _viewModel.FocusCharacterId.Value;
                
                // Highlight focused node
                if (_characterNodeElements.TryGetValue(focusCharacterId, out var focusNodeElement))
                {
                    focusNodeElement.AddToClassList("focus-node");
                }
                
                // Highlight relationships and related nodes
                foreach (var relationship in _viewModel.Relationships)
                {
                    bool isRelated = relationship.SourceCharacterID == focusCharacterId ||
                                     relationship.TargetCharacterID == focusCharacterId;
                    
                    if (isRelated)
                    {
                        string relationshipId = $"{relationship.SourceCharacterID}:{relationship.TargetCharacterID}";
                        if (_relationshipLineElements.TryGetValue(relationshipId, out var lineElement))
                        {
                            lineElement.AddToClassList("focus-relationship");
                        }
                        
                        // Highlight the related node
                        string relatedCharacterId = relationship.SourceCharacterID == focusCharacterId 
                            ? relationship.TargetCharacterID 
                            : relationship.SourceCharacterID;
                        
                        if (_characterNodeElements.TryGetValue(relatedCharacterId, out var relatedNodeElement))
                        {
                            relatedNodeElement.AddToClassList("related-node");
                        }
                    }
                }
            }
        }
        
        /// <summary>
        /// Applies styling based on character type
        /// </summary>
        /// <param name="element">The element to style</param>
        /// <param name="characterType">The character type</param>
        private void ApplyCharacterTypeStyle(VisualElement element, CharacterType characterType)
        {
            // Clear existing type classes
            foreach (CharacterType type in Enum.GetValues(typeof(CharacterType)))
            {
                element.RemoveFromClassList($"type-{type.ToString().ToLower()}");
            }
            
            // Add appropriate class
            element.AddToClassList($"type-{characterType.ToString().ToLower()}");
            
            // Set color based on character type
            switch (characterType)
            {
                case CharacterType.Player:
                    element.style.backgroundColor = new Color(0.1f, 0.6f, 0.9f); // Blue
                    break;
                case CharacterType.Ally:
                    element.style.backgroundColor = new Color(0.2f, 0.8f, 0.2f); // Green
                    break;
                case CharacterType.Neutral:
                    element.style.backgroundColor = new Color(0.8f, 0.8f, 0.2f); // Yellow
                    break;
                case CharacterType.Rival:
                    element.style.backgroundColor = new Color(0.9f, 0.5f, 0.1f); // Orange
                    break;
                case CharacterType.Enemy:
                    element.style.backgroundColor = new Color(0.9f, 0.2f, 0.2f); // Red
                    break;
                case CharacterType.NPC:
                    element.style.backgroundColor = new Color(0.7f, 0.7f, 0.7f); // Gray
                    break;
            }
        }
        
        /// <summary>
        /// Applies styling based on relationship type
        /// </summary>
        /// <param name="element">The element to style</param>
        /// <param name="relationshipType">The relationship type</param>
        private void ApplyRelationshipTypeStyle(VisualElement element, RelationshipType relationshipType)
        {
            // Clear existing type classes
            foreach (RelationshipType type in Enum.GetValues(typeof(RelationshipType)))
            {
                element.RemoveFromClassList($"type-{type.ToString().ToLower()}");
            }
            
            // Add appropriate class
            element.AddToClassList($"type-{relationshipType.ToString().ToLower()}");
            
            // Set background color based on relationship type
            element.style.backgroundColor = GetRelationshipTypeColor(relationshipType);
        }
        
        /// <summary>
        /// Gets the color for a relationship type
        /// </summary>
        /// <param name="relationshipType">The relationship type</param>
        /// <returns>The color for the relationship type</returns>
        private Color GetRelationshipTypeColor(RelationshipType relationshipType)
        {
            switch (relationshipType)
            {
                case RelationshipType.Friend:
                    return new Color(0.2f, 0.8f, 0.2f); // Green
                case RelationshipType.Family:
                    return new Color(0.2f, 0.6f, 0.9f); // Blue
                case RelationshipType.Romantic:
                    return new Color(0.9f, 0.2f, 0.6f); // Pink
                case RelationshipType.Colleague:
                    return new Color(0.6f, 0.6f, 0.9f); // Light Blue
                case RelationshipType.Ally:
                    return new Color(0.5f, 0.8f, 0.5f); // Light Green
                case RelationshipType.Rival:
                    return new Color(0.9f, 0.5f, 0.1f); // Orange
                case RelationshipType.Enemy:
                    return new Color(0.9f, 0.2f, 0.2f); // Red
                case RelationshipType.Acquaintance:
                    return new Color(0.8f, 0.8f, 0.2f); // Yellow
                case RelationshipType.Business:
                    return new Color(0.6f, 0.4f, 0.2f); // Brown
                default:
                    return new Color(0.7f, 0.7f, 0.7f); // Gray
            }
        }
        
        /// <summary>
        /// Converts graph coordinates to screen coordinates
        /// </summary>
        /// <param name="graphPosition">Position in graph coordinates</param>
        /// <returns>Position in screen coordinates</returns>
        private Vector2 GraphToScreenPosition(Vector2 graphPosition)
        {
            // Convert from graph coordinates to screen coordinates
            // Graph coordinates: (0,0) is center, (-1,-1) to (1,1) is the full graph
            // Screen coordinates: (0,0) is top left, (width,height) is bottom right
            
            float screenX = (graphPosition.x + 1) * 0.5f * _viewportSize.x;
            float screenY = (graphPosition.y + 1) * 0.5f * _viewportSize.y;
            
            return new Vector2(screenX, screenY);
        }
        
        /// <summary>
        /// Converts screen coordinates to graph coordinates
        /// </summary>
        /// <param name="screenPosition">Position in screen coordinates</param>
        /// <returns>Position in graph coordinates</returns>
        private Vector2 ScreenToGraphPosition(Vector2 screenPosition)
        {
            // Convert from screen coordinates to graph coordinates
            float graphX = (screenPosition.x / _viewportSize.x) * 2 - 1;
            float graphY = (screenPosition.y / _viewportSize.y) * 2 - 1;
            
            return new Vector2(graphX, graphY);
        }
        
        /// <summary>
        /// Handler for node mouse down events
        /// </summary>
        /// <param name="evt">The mouse event</param>
        /// <param name="character">The character node</param>
        private void OnNodeMouseDown(MouseDownEvent evt, CharacterNode character)
        {
            // Only handle left mouse button
            if (evt.button != 0) return;
            
            evt.StopPropagation();
            
            // Start dragging
            _isDragging = true;
            _draggedNode = character;
            _dragStartPosition = evt.mousePosition;
            
            // Calculate offset to keep the drag point relative to where the mouse was clicked on the node
            var nodeElement = _characterNodeElements[character.CharacterID];
            Vector2 nodeCenter = new Vector2(
                nodeElement.worldBound.x + nodeElement.worldBound.width / 2,
                nodeElement.worldBound.y + nodeElement.worldBound.height / 2
            );
            _nodeDragOffset = nodeCenter - _dragStartPosition;
        }
        
        /// <summary>
        /// Handler for node mouse up events
        /// </summary>
        /// <param name="evt">The mouse event</param>
        /// <param name="character">The character node</param>
        private void OnNodeMouseUp(MouseUpEvent evt, CharacterNode character)
        {
            if (!_isDragging || _draggedNode?.CharacterID != character.CharacterID) return;
            
            evt.StopPropagation();
            
            // End dragging and update node position in the view model
            _isDragging = false;
            
            // Update position in the view model
            UpdateNodePosition(_draggedNode, evt.mousePosition + _nodeDragOffset);
            
            _draggedNode = null;
        }
        
        /// <summary>
        /// Handler for node click events
        /// </summary>
        /// <param name="evt">The click event</param>
        /// <param name="character">The character node</param>
        private void OnNodeClick(ClickEvent evt, CharacterNode character)
        {
            evt.StopPropagation();
            
            // Select the character in the view model
            _viewModel.SelectCharacterCommand.Execute(character.CharacterID);
        }
        
        /// <summary>
        /// Handler for viewport mouse down events
        /// </summary>
        /// <param name="evt">The mouse event</param>
        private void OnViewportMouseDown(MouseDownEvent evt)
        {
            // If not dragging a node and middle mouse button, start panning
            if (!_isDragging && evt.button == 2)
            {
                _dragStartPosition = evt.mousePosition;
            }
        }
        
        /// <summary>
        /// Handler for viewport mouse move events
        /// </summary>
        /// <param name="evt">The mouse event</param>
        private void OnViewportMouseMove(MouseMoveEvent evt)
        {
            if (_isDragging && _draggedNode != null)
            {
                // Update node position for dragging
                Vector2 newMousePos = evt.mousePosition;
                UpdateNodePosition(_draggedNode, newMousePos + _nodeDragOffset);
            }
            else if (evt.button == 2) // Middle mouse button for panning
            {
                // Calculate pan distance
                Vector2 delta = evt.mousePosition - _dragStartPosition;
                _dragStartPosition = evt.mousePosition;
                
                // Apply pan
                Vector2 currentPan = _viewModel.PanPosition.Value;
                Vector2 newPan = currentPan + delta;
                _viewModel.SetPanPosition(newPan);
            }
        }
        
        /// <summary>
        /// Handler for viewport mouse up events
        /// </summary>
        /// <param name="evt">The mouse event</param>
        private void OnViewportMouseUp(MouseUpEvent evt)
        {
            // End dragging if we were dragging a node
            if (_isDragging && _draggedNode != null && evt.button == 0)
            {
                _isDragging = false;
                
                // Update position in the view model
                UpdateNodePosition(_draggedNode, evt.mousePosition + _nodeDragOffset);
                
                _draggedNode = null;
            }
        }
        
        /// <summary>
        /// Handler for viewport wheel events
        /// </summary>
        /// <param name="evt">The wheel event</param>
        private void OnViewportWheel(WheelEvent evt)
        {
            evt.StopPropagation();
            
            // Adjust zoom based on wheel delta
            float zoomDelta = evt.delta.y * -0.01f;
            float newZoom = Mathf.Clamp(_viewModel.ZoomLevel.Value + zoomDelta, 0.5f, 2.0f);
            _viewModel.SetZoomLevel(newZoom);
        }
        
        /// <summary>
        /// Updates a node's position
        /// </summary>
        /// <param name="node">The node to update</param>
        /// <param name="screenPosition">The new screen position</param>
        private void UpdateNodePosition(CharacterNode node, Vector2 screenPosition)
        {
            // Convert screen position to graph position
            Vector2 graphPosition = ScreenToGraphPosition(screenPosition);
            
            // Update the node position in the view model
            _viewModel.MoveCharacter(node.CharacterID, graphPosition);
            
            // Update the node element position immediately for smooth dragging
            var nodeElement = _characterNodeElements[node.CharacterID];
            nodeElement.style.left = screenPosition.x - (NODE_SIZE / 2);
            nodeElement.style.top = screenPosition.y - (NODE_SIZE / 2);
            
            // Also update relationships
            foreach (var relationship in _viewModel.Relationships)
            {
                if (relationship.SourceCharacterID == node.CharacterID || 
                    relationship.TargetCharacterID == node.CharacterID)
                {
                    string relationshipId = $"{relationship.SourceCharacterID}:{relationship.TargetCharacterID}";
                    if (_relationshipLineElements.TryGetValue(relationshipId, out var lineElement))
                    {
                        // Find the other character in the relationship
                        CharacterNode sourceNode = null;
                        CharacterNode targetNode = null;
                        
                        if (relationship.SourceCharacterID == node.CharacterID)
                        {
                            sourceNode = node;
                            targetNode = _viewModel.GetCharacter(relationship.TargetCharacterID);
                        }
                        else
                        {
                            sourceNode = _viewModel.GetCharacter(relationship.SourceCharacterID);
                            targetNode = node;
                        }
                        
                        if (sourceNode != null && targetNode != null)
                        {
                            // Update the line position
                            UpdateRelationshipLine(lineElement, relationship, sourceNode, targetNode);
                        }
                    }
                }
            }
            
            // Also update any groups
            foreach (var group in _viewModel.Groups)
            {
                if (group.MemberIDs.Contains(node.CharacterID))
                {
                    string groupId = group.GroupID;
                    if (_groupBoundaryElements.TryGetValue(groupId, out var groupElement))
                    {
                        UpdateGroupBoundary(groupElement, group);
                    }
                }
            }
        }
    }
}